package results

import (
	"fmt"
	"os"
	"path/filepath"

	"stockcentraluploadlistproductsv2/internal/reporting"
	"stockcentraluploadlistproductsv2/internal/workbook"

	"github.com/xuri/excelize/v2"
)

// Este archivo centraliza la escritura de los excels de salida del batch.
//
// Por ahora soporta dos artefactos:
// - `Resultados`: una fila por SKU con estado y explicación
// - `ErroresEstructura`: una fila por error estructural del Excel
//
// La intención es que estos archivos sean legibles por negocio y también
// suficientemente estables para automatizaciones futuras.

// Writer encapsula la generación de excels de salida.
type Writer struct{}

// NewWriter crea un escritor simple de resultados.
func NewWriter() *Writer {
	return &Writer{}
}

// WriteRowResults genera el archivo de resultados por SKU.
func (w *Writer) WriteRowResults(path string, rows []reporting.RowResult) error {
	file := excelize.NewFile()

	const sheet = "Resultados"
	file.SetSheetName(file.GetSheetName(0), sheet)

	// Estos encabezados buscan que negocio pueda leer el archivo sin conocer
	// detalles internos del proceso.
	headers := []string{
		"Fila Excel",
		"SKU",
		"Estado",
		"Producto",
		"Imagenes",
		"Mensaje",
		"Detalle",
	}

	for idx, header := range headers {
		cell, _ := excelize.CoordinatesToCellName(idx+1, 1)
		if err := file.SetCellValue(sheet, cell, header); err != nil {
			return fmt.Errorf("set results header %q: %w", header, err)
		}
	}

	// `dataRow` lleva la fila real dentro del Excel de salida.
	dataRow := 2
	for _, row := range rows {
		// Las filas SKIPPED no se escriben para que el archivo final quede
		// enfocado solo en SKUs reales para negocio.
		if row.Status == reporting.RowStatusSkipped {
			continue
		}

		excelRow := dataRow

		values := []any{
			row.ExcelRowNumber,
			row.SKU,
			string(row.Status),
			row.ProductResult,
			row.ImagesResult,
			row.Message,
			row.Detail,
		}

		// Volcamos la fila completa columna por columna para mantener un orden fijo.
		for col, value := range values {
			cell, _ := excelize.CoordinatesToCellName(col+1, excelRow)
			if err := file.SetCellValue(sheet, cell, value); err != nil {
				return fmt.Errorf("set results cell row=%d col=%d: %w", excelRow, col+1, err)
			}
		}

		dataRow++
	}

	// El estilo se aplica al final, cuando ya sabemos hasta qué fila escribimos.
	if err := w.styleResultsSheet(file, sheet, dataRow-1); err != nil {
		return err
	}
	w.adjustColumns(file, sheet, headers)
	return w.save(file, path)
}

// WriteStructureErrors genera el archivo de errores estructurales.
func (w *Writer) WriteStructureErrors(path string, errors []workbook.StructureError) error {
	file := excelize.NewFile()

	const sheet = "ErroresEstructura"
	file.SetSheetName(file.GetSheetName(0), sheet)

	// Esta hoja es más corta: apunta a explicar por qué el layout fue rechazado.
	headers := []string{"Campo", "Mensaje", "Detalle"}
	for idx, header := range headers {
		cell, _ := excelize.CoordinatesToCellName(idx+1, 1)
		if err := file.SetCellValue(sheet, cell, header); err != nil {
			return fmt.Errorf("set structure header %q: %w", header, err)
		}
	}

	for idx, issue := range errors {
		excelRow := idx + 2
		values := []any{issue.Field, issue.Message, issue.Detail}

		// Igual que en resultados, mantenemos una escritura explícita por columna.
		for col, value := range values {
			cell, _ := excelize.CoordinatesToCellName(col+1, excelRow)
			if err := file.SetCellValue(sheet, cell, value); err != nil {
				return fmt.Errorf("set structure cell row=%d col=%d: %w", excelRow, col+1, err)
			}
		}
	}

	if err := w.styleStructureSheet(file, sheet, len(errors)+1); err != nil {
		return err
	}
	w.adjustColumns(file, sheet, headers)
	return w.save(file, path)
}

// save crea la carpeta padre si hace falta y persiste el workbook.
func (w *Writer) save(file *excelize.File, path string) error {
	defer func() {
		_ = file.Close()
	}()

	// Creamos la carpeta si todavía no existe para que SaveAs no falle por ruta.
	if err := os.MkdirAll(filepath.Dir(path), 0o755); err != nil {
		return fmt.Errorf("create result parent directory: %w", err)
	}

	if err := file.SaveAs(path); err != nil {
		return fmt.Errorf("save workbook %s: %w", path, err)
	}

	return nil
}

// adjustColumns aplica un ancho simple para dejar el archivo más legible.
func (w *Writer) adjustColumns(file *excelize.File, sheet string, _ []string) {
	_ = file.SetColWidth(sheet, "A", "A", 12)
	_ = file.SetColWidth(sheet, "B", "B", 18)
	_ = file.SetColWidth(sheet, "C", "C", 14)
	_ = file.SetColWidth(sheet, "D", "E", 16)
	_ = file.SetColWidth(sheet, "F", "F", 34)
	_ = file.SetColWidth(sheet, "G", "G", 70)
}

// styleResultsSheet aplica solo el formato útil y sobrio para lectura humana.
func (w *Writer) styleResultsSheet(file *excelize.File, sheet string, lastRow int) error {
	// Header azul para que el usuario identifique rápido la zona de títulos.
	headerStyle, err := file.NewStyle(&excelize.Style{
		Font:      &excelize.Font{Bold: true, Color: "#FFFFFF"},
		Fill:      excelize.Fill{Type: "pattern", Color: []string{"#1F4E78"}, Pattern: 1},
		Alignment: &excelize.Alignment{Horizontal: "center", Vertical: "center"},
	})
	if err != nil {
		return fmt.Errorf("create results header style: %w", err)
	}

	textWrapStyle, err := file.NewStyle(&excelize.Style{
		Alignment: &excelize.Alignment{Vertical: "top", WrapText: true},
	})
	if err != nil {
		return fmt.Errorf("create results wrap style: %w", err)
	}

	okStyle, err := file.NewStyle(&excelize.Style{
		Fill:      excelize.Fill{Type: "pattern", Color: []string{"#E2F0D9"}, Pattern: 1},
		Alignment: &excelize.Alignment{Vertical: "top"},
	})
	if err != nil {
		return fmt.Errorf("create results ok style: %w", err)
	}

	partialStyle, err := file.NewStyle(&excelize.Style{
		Fill:      excelize.Fill{Type: "pattern", Color: []string{"#FFF2CC"}, Pattern: 1},
		Alignment: &excelize.Alignment{Vertical: "top"},
	})
	if err != nil {
		return fmt.Errorf("create results partial style: %w", err)
	}

	errorStyle, err := file.NewStyle(&excelize.Style{
		Fill:      excelize.Fill{Type: "pattern", Color: []string{"#F4CCCC"}, Pattern: 1},
		Alignment: &excelize.Alignment{Vertical: "top"},
	})
	if err != nil {
		return fmt.Errorf("create results error style: %w", err)
	}

	if err := file.SetCellStyle(sheet, "A1", "G1", headerStyle); err != nil {
		return fmt.Errorf("set results header style: %w", err)
	}
	if lastRow >= 2 {
		// El cuerpo usa wrap text porque mensaje y detalle pueden ser largos.
		if err := file.SetCellStyle(sheet, "A2", fmt.Sprintf("G%d", lastRow), textWrapStyle); err != nil {
			return fmt.Errorf("set results body style: %w", err)
		}
	}

	// Freeze y filtro ayudan a navegar archivos grandes sin perder contexto.
	_ = file.SetPanes(sheet, &excelize.Panes{
		Freeze:      true,
		Split:       false,
		XSplit:      0,
		YSplit:      1,
		TopLeftCell: "A2",
		ActivePane:  "bottomLeft",
	})
	_ = file.AutoFilter(sheet, "A1:G1", []excelize.AutoFilterOptions{})

	for row := 2; row <= lastRow; row++ {
		statusCell := fmt.Sprintf("C%d", row)
		value, _ := file.GetCellValue(sheet, statusCell)

		// Solo coloreamos la celda de estado para que el archivo siga sobrio.
		switch value {
		case string(reporting.RowStatusOK):
			_ = file.SetCellStyle(sheet, statusCell, statusCell, okStyle)
		case string(reporting.RowStatusPartialOK):
			_ = file.SetCellStyle(sheet, statusCell, statusCell, partialStyle)
		case string(reporting.RowStatusError):
			_ = file.SetCellStyle(sheet, statusCell, statusCell, errorStyle)
		}
	}

	return nil
}

// styleStructureSheet aplica un formato sobrio y legible a la hoja de errores
// estructurales para que el archivo sea fácil de leer por negocio.
func (w *Writer) styleStructureSheet(file *excelize.File, sheet string, lastRow int) error {
	// La hoja estructural usa otro color para diferenciarla visualmente
	// del Excel de resultados por fila.
	headerStyle, err := file.NewStyle(&excelize.Style{
		Font:      &excelize.Font{Bold: true, Color: "#FFFFFF"},
		Fill:      excelize.Fill{Type: "pattern", Color: []string{"#7F6000"}, Pattern: 1},
		Alignment: &excelize.Alignment{Horizontal: "center", Vertical: "center"},
	})
	if err != nil {
		return fmt.Errorf("create structure header style: %w", err)
	}

	bodyStyle, err := file.NewStyle(&excelize.Style{
		Alignment: &excelize.Alignment{Vertical: "top", WrapText: true},
	})
	if err != nil {
		return fmt.Errorf("create structure body style: %w", err)
	}

	if err := file.SetCellStyle(sheet, "A1", "C1", headerStyle); err != nil {
		return fmt.Errorf("set structure header style: %w", err)
	}
	if lastRow >= 2 {
		if err := file.SetCellStyle(sheet, "A2", fmt.Sprintf("C%d", lastRow), bodyStyle); err != nil {
			return fmt.Errorf("set structure body style: %w", err)
		}
	}

	// Igual que en resultados, dejamos columnas, freeze y filtro listos
	// para lectura humana desde Excel.
	_ = file.SetColWidth(sheet, "A", "A", 20)
	_ = file.SetColWidth(sheet, "B", "B", 34)
	_ = file.SetColWidth(sheet, "C", "C", 70)
	_ = file.SetPanes(sheet, &excelize.Panes{
		Freeze:      true,
		Split:       false,
		XSplit:      0,
		YSplit:      1,
		TopLeftCell: "A2",
		ActivePane:  "bottomLeft",
	})
	_ = file.AutoFilter(sheet, "A1:C1", []excelize.AutoFilterOptions{})

	return nil
}

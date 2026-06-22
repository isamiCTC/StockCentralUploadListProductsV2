package workbook

import (
	"fmt"
	"math"
	"net/url"
	"strings"
	"time"
)

// Este archivo transforma filas crudas del workbook en DTOs tipados
// respetando las reglas funcionales observadas en el legado.
//
// Responsabilidad del archivo:
// - leer una `RawRow`
// - ubicar sus celdas por nombre lógico de columna
// - parsear y normalizar valores
// - devolver un DTO útil para negocio o una lista clara de problemas
//
// En esta etapa todavía no llama a la API ni resuelve categorías externas.
// Su trabajo es dejar la fila "lista para negocio" o explicar claramente
// por qué no pudo hacerlo.

// MapRows convierte todas las filas del workbook al tipo correspondiente
// según el formato detectado.
//
// Este es el punto de entrada principal del mapper.
func MapRows(workbook Workbook) ([]MappedRow, error) {
	// El formato detectado antes define qué DTO espera cada fila.
	switch workbook.Format {
	case FileFormatStockUpdate:
		return mapStockUpdateRows(workbook), nil
	case FileFormatFullImport:
		return mapFullImportRows(workbook), nil
	default:
		return nil, fmt.Errorf("format %s does not support row mapping", workbook.Format)
	}
}

// mapStockUpdateRows implementa el formato simple de 2 columnas.
// La regla es deliberadamente mínima: SKU + stock.
func mapStockUpdateRows(workbook Workbook) []MappedRow {
	rows := make([]MappedRow, 0, len(workbook.Rows))

	for _, rawRow := range workbook.Rows {
		// Arrancamos con una estructura vacía y la vamos completando.
		mapped := MappedRow{
			ExcelRowNumber: rawRow.ExcelRowNumber,
			IsEmpty:        rawRow.IsEmpty,
		}

		// Las filas vacías se conservan como "vacías" para que la capa superior
		// pueda contarlas como skipped sin tratarlas como error.
		if rawRow.IsEmpty {
			rows = append(rows, mapped)
			continue
		}

		// Tomamos valores por nombre lógico de columna, no por índice fijo.
		sku := getCellValue(rawRow, workbook.HeaderIndexByKey, "SKU")
		stock, stockErr := ParseFlexibleInt(getCellValue(rawRow, workbook.HeaderIndexByKey, "STOCK"))

		// Guardamos el SKU aunque la fila tenga errores para poder identificarla.
		mapped.SKU = sku
		if sku == "" {
			mapped.Issues = append(mapped.Issues, RowIssue{
				Severity: "ERROR",
				Field:    "SKU",
				Message:  "SKU obligatorio faltante",
				Detail:   "",
			})
		}
		validateSKUFormat(&mapped, sku)
		if stockErr != nil {
			mapped.Issues = append(mapped.Issues, RowIssue{
				Severity: "ERROR",
				Field:    "STOCK",
				Message:  "Stock inválido",
				Detail:   stockErr.Error(),
			})
		}

		// Solo si todo salió bien construimos el DTO consumible por negocio.
		if !mapped.HasErrors() {
			mapped.StockUpdate = &StockUpdateRow{
				SKU:   sku,
				Stock: stock,
			}
		}

		rows = append(rows, mapped)
	}

	return rows
}

// mapFullImportRows implementa el formato completo de 19 columnas.
// Acá vive la mayor parte de las reglas heredadas del servicio original.
func mapFullImportRows(workbook Workbook) []MappedRow {
	rows := make([]MappedRow, 0, len(workbook.Rows))

	for _, rawRow := range workbook.Rows {
		// Igual que en stock update, cada fila arranca como una estructura vacía
		// que después puede terminar en DTO o en lista de issues.
		mapped := MappedRow{
			ExcelRowNumber: rawRow.ExcelRowNumber,
			IsEmpty:        rawRow.IsEmpty,
		}

		// Igual que en el formato simple, las filas vacías no son error.
		if rawRow.IsEmpty {
			rows = append(rows, mapped)
			continue
		}

		// Primero levantamos todos los campos textuales "base".
		// Esto mantiene el mapping legible antes de empezar con parseos.
		sku := getCellValue(rawRow, workbook.HeaderIndexByKey, "SKU")
		name := getCellValue(rawRow, workbook.HeaderIndexByKey, "NOMBRE")
		brand := getCellValue(rawRow, workbook.HeaderIndexByKey, "MARCA")
		description := getCellValue(rawRow, workbook.HeaderIndexByKey, "DESCRIPCION")
		productType := getCellValue(rawRow, workbook.HeaderIndexByKey, "TIPO")
		ahora := getCellValue(rawRow, workbook.HeaderIndexByKey, "AHORA")
		category := getCellValue(rawRow, workbook.HeaderIndexByKey, "CATEGORIA")
		subCategory := getCellValue(rawRow, workbook.HeaderIndexByKey, "SUB CATEGORIA")
		startDateRaw := getCellValue(rawRow, workbook.HeaderIndexByKey, "FECHA DE INICIO")
		endDateRaw := getCellValue(rawRow, workbook.HeaderIndexByKey, "FECHA DE FIN")

		mapped.SKU = sku

		// Después validamos los textos obligatorios.
		// Esta etapa solo mira presencia de datos, no parsea números todavía.
		validateRequiredText(&mapped, "SKU", sku)
		validateSKUFormat(&mapped, sku)
		validateRequiredText(&mapped, "NOMBRE", name)
		validateRequiredText(&mapped, "MARCA", brand)
		validateRequiredText(&mapped, "DESCRIPCION", description)
		validateRequiredText(&mapped, "CATEGORIA", category)
		validateRequiredText(&mapped, "SUB CATEGORIA", subCategory)
		startDate, startDateOK := parseOptionalDateField(&mapped, "FECHA DE INICIO", startDateRaw)
		endDate, endDateOK := parseOptionalDateField(&mapped, "FECHA DE FIN", endDateRaw)
		validateDateRange(&mapped, startDateRaw, startDateOK, startDate, endDateRaw, endDateOK, endDate)

		// Luego parseamos todos los campos numéricos relevantes.
		// Cada parseo agrega su propio issue si falla.
		height, heightOK := parseRequiredFloatField(&mapped, "ALTO", getCellValue(rawRow, workbook.HeaderIndexByKey, "ALTO"))
		width, widthOK := parseRequiredFloatField(&mapped, "ANCHO", getCellValue(rawRow, workbook.HeaderIndexByKey, "ANCHO"))
		depth, depthOK := parseRequiredFloatField(&mapped, "LARGO", getCellValue(rawRow, workbook.HeaderIndexByKey, "LARGO"))
		weightGrams, weightOK := parseRequiredFloatField(&mapped, "PESO", getCellValue(rawRow, workbook.HeaderIndexByKey, "PESO"))
		price, _ := parseRequiredFloatField(&mapped, "PRECIO", getCellValue(rawRow, workbook.HeaderIndexByKey, "PRECIO"))
		taxes, taxesOK := parseRequiredFloatField(&mapped, "IVA", getCellValue(rawRow, workbook.HeaderIndexByKey, "IVA"))
		stock, _ := parseRequiredIntField(&mapped, "STOCK", getCellValue(rawRow, workbook.HeaderIndexByKey, "STOCK"))

		// El campo de imágenes tiene semántica propia:
		// vacío no es error; URL inválida sí.
		imageURLs, imageIssues := normalizeImageURLs(getCellValue(rawRow, workbook.HeaderIndexByKey, "URL IMAGENES"))
		mapped.Issues = append(mapped.Issues, imageIssues...)

		// La oferta es opcional. Si viene y es válida, puede sobrescribir `Price`.
		// También guardamos el valor original para list price y net price.
		var offerPtr *float64
		offerApplied := false
		listPrice := price
		netPrice := price

		if offerValue, issue := parseOptionalOffer(getCellValue(rawRow, workbook.HeaderIndexByKey, "OFERTA")); issue != nil {
			mapped.Issues = append(mapped.Issues, *issue)
		} else if offerValue != nil {
			offerPtr = offerValue
			if *offerValue > 0 {
				price = *offerValue
				offerApplied = true
			}
		}

		// Regla heredada: IVA entre 0 y 1 se interpreta como fracción y se pasa
		// a porcentaje.
		if taxesOK && taxes > 0 && taxes < 1 {
			taxes = taxes * 100
		}

		// Regla heredada: el Excel trae peso en gramos y la API consume kilogramos.
		// Si el peso ya falló al parsear, dejamos el valor final en cero.
		weightKilograms := 0.0
		if weightOK {
			weightKilograms = roundToTwo(weightGrams) / 1000
		}

		// Redondeamos dimensiones y peso tal como hace el legado conceptualmente.
		if heightOK {
			height = roundToTwo(height)
		}
		if widthOK {
			width = roundToTwo(width)
		}
		if depthOK {
			depth = roundToTwo(depth)
		}

		// El DTO final solo se arma si la fila quedó libre de errores.
		if !mapped.HasErrors() {
			// A esta altura ya tenemos todo lo necesario para dejar la fila
			// lista para la capa de negocio.
			mapped.FullImport = &FullImportRow{
				SKU:              sku,
				Name:             name,
				Brand:            brand,
				Description:      description,
				ShortDescription: name,
				Height:           height,
				Width:            width,
				Depth:            depth,
				WeightGrams:      roundToTwo(weightGrams),
				WeightKilograms:  weightKilograms,
				ImageURLs:        imageURLs,
				HasImages:        len(imageURLs) > 0,
				Price:            price,
				ListPrice:        listPrice,
				NetPrice:         netPrice,
				Taxes:            taxes,
				Type:             productType,
				Ahora:            ahora,
				Category:         category,
				SubCategory:      subCategory,
				Stock:            stock,
				Offer:            offerPtr,
				OfferApplied:     offerApplied,
				StartDateRaw:     startDateRaw,
				EndDateRaw:       endDateRaw,
				SyncImages:       len(imageURLs) > 0,
			}
		}

		rows = append(rows, mapped)
	}

	return rows
}

func validateRequiredText(mapped *MappedRow, field, value string) {
	// Si hay contenido, no agregamos issue.
	if value != "" {
		return
	}

	mapped.Issues = append(mapped.Issues, RowIssue{
		Severity: "ERROR",
		Field:    field,
		Message:  "Campo obligatorio faltante",
		Detail:   "",
	})
}

// validateSKUFormat agrega una regla explícita de caracteres permitidos
// para evitar que lleguen SKUs incompatibles a las rutas de la API.
func validateSKUFormat(mapped *MappedRow, sku string) {
	if sku == "" {
		return
	}

	invalidChar, ok := findInvalidSKUChar(sku)
	if !ok {
		return
	}

	mapped.Issues = append(mapped.Issues, RowIssue{
		Severity: "ERROR",
		Field:    "SKU",
		Message:  "SKU inválido",
		Detail:   fmt.Sprintf("Carácter inválido en SKU: %q", string(invalidChar)),
	})
}

// findInvalidSKUChar devuelve el primer carácter que no cumple la whitelist
// acordada para SKU: ASCII alfanumérico, guion o guion bajo.
func findInvalidSKUChar(sku string) (rune, bool) {
	for _, r := range sku {
		if r >= 'a' && r <= 'z' {
			continue
		}
		if r >= 'A' && r <= 'Z' {
			continue
		}
		if r >= '0' && r <= '9' {
			continue
		}
		if r == '-' || r == '_' {
			continue
		}
		return r, true
	}

	return 0, false
}

// parseOptionalDateField valida fechas opcionales con formato estricto
// `DD/MM/YYYY`. Si el valor viene vacío, no agrega error.
func parseOptionalDateField(mapped *MappedRow, field, raw string) (time.Time, bool) {
	if raw == "" {
		return time.Time{}, false
	}

	value, err := time.Parse("02/01/2006", raw)
	if err != nil {
		mapped.Issues = append(mapped.Issues, RowIssue{
			Severity: "ERROR",
			Field:    field,
			Message:  "Fecha inválida",
			Detail:   fmt.Sprintf("Valor inválido: %q. Debe tener formato DD/MM/YYYY", raw),
		})
		return time.Time{}, false
	}

	return value, true
}

// validateDateRange solo se activa si ambas fechas opcionales vienen cargadas
// y pudieron parsearse correctamente.
func validateDateRange(mapped *MappedRow, startRaw string, startOK bool, start time.Time, endRaw string, endOK bool, end time.Time) {
	if startRaw == "" || endRaw == "" {
		return
	}
	if !startOK || !endOK {
		return
	}
	if !start.After(end) {
		return
	}

	mapped.Issues = append(mapped.Issues, RowIssue{
		Severity: "ERROR",
		Field:    "FECHA DE FIN",
		Message:  "Rango de fechas inválido",
		Detail:   fmt.Sprintf("FECHA DE INICIO (%s) no puede ser posterior a FECHA DE FIN (%s)", startRaw, endRaw),
	})
}

// parseRequiredFloatField centraliza la validación de numéricos obligatorios.
func parseRequiredFloatField(mapped *MappedRow, field, raw string) (float64, bool) {
	if raw == "" {
		mapped.Issues = append(mapped.Issues, RowIssue{
			Severity: "ERROR",
			Field:    field,
			Message:  "Campo numérico obligatorio faltante",
			Detail:   "",
		})
		return 0, false
	}

	value, err := ParseFlexibleFloat(raw)
	if err != nil {
		mapped.Issues = append(mapped.Issues, RowIssue{
			Severity: "ERROR",
			Field:    field,
			Message:  "Valor numérico inválido",
			Detail:   err.Error(),
		})
		return 0, false
	}

	return value, true
}

// parseRequiredIntField centraliza la validación de enteros obligatorios.
func parseRequiredIntField(mapped *MappedRow, field, raw string) (int, bool) {
	if raw == "" {
		mapped.Issues = append(mapped.Issues, RowIssue{
			Severity: "ERROR",
			Field:    field,
			Message:  "Campo entero obligatorio faltante",
			Detail:   "",
		})
		return 0, false
	}

	value, err := ParseFlexibleInt(raw)
	if err != nil {
		mapped.Issues = append(mapped.Issues, RowIssue{
			Severity: "ERROR",
			Field:    field,
			Message:  "Valor entero inválido",
			Detail:   err.Error(),
		})
		return 0, false
	}

	return value, true
}

// parseOptionalOffer interpreta la oferta solo si el campo viene con contenido.
// Si viene vacío, no genera error.
func parseOptionalOffer(raw string) (*float64, *RowIssue) {
	if strings.TrimSpace(raw) == "" {
		return nil, nil
	}

	value, err := ParseFlexibleFloat(raw)
	if err != nil {
		return nil, &RowIssue{
			Severity: "ERROR",
			Field:    "OFERTA",
			Message:  "Oferta inválida",
			Detail:   err.Error(),
		}
	}

	return &value, nil
}

// normalizeImageURLs aplica la regla acordada para el campo de imágenes:
// - trim global
// - split por `&`
// - trim por segmento
// - descartar vacíos
// - validar URL
func normalizeImageURLs(raw string) ([]string, []RowIssue) {
	clean := strings.TrimSpace(raw)
	if clean == "" {
		return nil, nil
	}

	// El legado usa `&` como separador de múltiples imágenes en una sola celda.
	segments := strings.Split(clean, "&")
	urls := make([]string, 0, len(segments))
	issues := make([]RowIssue, 0)

	for _, segment := range segments {
		value := strings.TrimSpace(segment)
		// Un segmento vacío entre separadores no se considera error.
		if value == "" {
			continue
		}

		// Cada URL inválida genera issue, pero no rompe el análisis del resto.
		if err := validateImageURL(value); err != nil {
			issues = append(issues, RowIssue{
				Severity: "ERROR",
				Field:    "URL IMAGENES",
				Message:  "URL de imagen inválida",
				Detail:   err.Error(),
			})
			continue
		}

		urls = append(urls, value)
	}

	if len(urls) == 0 && len(issues) == 0 {
		return nil, nil
	}

	return urls, issues
}

// validateImageURL restringe el formato a URLs HTTP/HTTPS razonables.
func validateImageURL(value string) error {
	parsed, err := url.ParseRequestURI(value)
	if err != nil {
		return fmt.Errorf("la URL %q no es válida: %w", value, err)
	}

	scheme := strings.ToLower(parsed.Scheme)
	if scheme != "http" && scheme != "https" {
		return fmt.Errorf("la URL %q debe usar http o https", value)
	}

	if parsed.Host == "" {
		return fmt.Errorf("la URL %q no tiene host", value)
	}

	return nil
}

// getCellValue busca una celda por nombre lógico de columna usando el índice
// normalizado construido por la validación estructural.
func getCellValue(row RawRow, headerIndexByKey map[string]int, field string) string {
	index, ok := headerIndexByKey[NormalizeHeader(field)]
	if !ok {
		// Si la columna no existe en el índice, devolvemos vacío y dejamos
		// que la validación superior lo trate.
		return ""
	}
	if index >= len(row.Values) {
		// Esto protege contra filas más cortas que el header.
		return ""
	}

	return NormalizeCell(row.Values[index])
}

// roundToTwo replica el redondeo a dos decimales usado en varias reglas legacy.
func roundToTwo(value float64) float64 {
	return math.Round(value*100) / 100
}

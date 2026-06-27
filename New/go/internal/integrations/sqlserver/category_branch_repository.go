package sqlserver

import (
	"context"
	"database/sql"
	"fmt"
	"strconv"
	"strings"
	"time"

	"stockcentraluploadlistproductsv2/internal/catalog"
	appconfig "stockcentraluploadlistproductsv2/internal/config"
	productsapi "stockcentraluploadlistproductsv2/internal/integrations/productsapi"
)

const defaultCategoryMappingsSPName = "CatalogCategoryBranchLookup_Get"

// Este archivo implementa la carga de ramas válidas de categoría desde SQL Server.
//
// Su responsabilidad es ejecutar el SP del catálogo vigente y transformar el
// resultset en una cache en memoria indexada por nombre normalizado.
type CategoryBranchRepository struct {
	server     *SQLServer
	spName     string
	timeoutSec int
}

// NewCategoryBranchRepository recibe la conexión SQL ya abierta y el nombre
// del SP desde configuración.
func NewCategoryBranchRepository(server *SQLServer, cfg appconfig.DatabaseConfig) *CategoryBranchRepository {
	return &CategoryBranchRepository{
		server:     server,
		spName:     firstNonEmpty(strings.TrimSpace(cfg.CategoryMappingsSPName), defaultCategoryMappingsSPName),
		timeoutSec: cfg.TimeoutSeconds,
	}
}

// LoadCatalogMappings trae las categorías válidas del catálogo y arma el índice
// por nombre normalizado para resolver subcategorías del Excel en memoria.
func (r *CategoryBranchRepository) LoadCatalogMappings(ctx context.Context, catalogID int) (map[string]productsapi.CategoryBranch, error) {
	queryCtx, cancel := context.WithTimeout(ctx, time.Duration(r.timeoutSec)*time.Second)
	defer cancel()

	// Ejecutamos el SP en forma explícita para dejar visible que el dataset
	// depende del `catalog_id` configurado en el batch.
	rows, err := r.server.QueryContext(
		queryCtx,
		fmt.Sprintf("EXEC %s @CatalogoId = @p1", r.spName),
		catalogID,
	)
	if err != nil {
		return nil, fmt.Errorf("execute category mappings stored procedure %s: %w", r.spName, err)
	}
	defer rows.Close()

	columns, err := rows.Columns()
	if err != nil {
		return nil, fmt.Errorf("read category mapping columns: %w", err)
	}

	// Buscamos columnas por nombre para no depender del orden del resultset.
	columnIndex := make(map[string]int, len(columns))
	for i, column := range columns {
		columnIndex[strings.ToUpper(strings.TrimSpace(column))] = i
	}

	nameIndex, ok := columnIndex["NAME"]
	if !ok {
		return nil, fmt.Errorf("category mapping resultset does not include Name column")
	}

	rubroIndex, ok := columnIndex["CODE"]
	if !ok {
		return nil, fmt.Errorf("category mapping resultset does not include Code column")
	}

	normalizedIndex, ok := columnIndex["NORMALIZEDNAME"]
	if !ok {
		return nil, fmt.Errorf("category mapping resultset does not include NormalizedName column")
	}

	// La cache final queda indexada por clave normalizada para que el resolvedor
	// pueda comparar rápido contra `SUB CATEGORIA`.
	mappings := make(map[string]productsapi.CategoryBranch)
	for rows.Next() {
		key, branch, scanErr := scanCategoryMappingRow(rows, nameIndex, rubroIndex, normalizedIndex, len(columns))
		if scanErr != nil {
			return nil, scanErr
		}
		if key == "" {
			continue
		}
		// Si dos filas colapsan a la misma clave normalizada, preferimos fallar
		// temprano antes que resolver categorías ambiguas en silencio.
		if existing, exists := mappings[key]; exists {
			return nil, fmt.Errorf("duplicate normalized category name %q for codes %s and %s", key, existing.Code, branch.Code)
		}
		mappings[key] = branch
	}

	if err := rows.Err(); err != nil {
		return nil, fmt.Errorf("iterate category mapping rows: %w", err)
	}

	return mappings, nil
}

// scanCategoryMappingRow toma una fila ya posicionada y extrae exactamente los
// tres datos que el resolvedor necesita: clave, código y nombre visible.
func scanCategoryMappingRow(rows *sql.Rows, nameIndex, rubroIndex, normalizedIndex, columnsCount int) (string, productsapi.CategoryBranch, error) {
	rawValues := make([]sql.RawBytes, columnsCount)
	destinations := make([]any, columnsCount)
	for i := range rawValues {
		// Usamos un slice paralelo de punteros para poder leer un resultset
		// dinámico sin atarnos a tipos columna por columna.
		destinations[i] = &rawValues[i]
	}

	if err := rows.Scan(destinations...); err != nil {
		return "", productsapi.CategoryBranch{}, fmt.Errorf("scan category mapping row: %w", err)
	}

	name := strings.TrimSpace(string(rawValues[nameIndex]))
	if name == "" {
		return "", productsapi.CategoryBranch{}, nil
	}

	code := strings.TrimSpace(string(rawValues[rubroIndex]))
	if code == "" {
		return "", productsapi.CategoryBranch{}, nil
	}

	normalizedName := catalog.NormalizeCategoryKey(string(rawValues[normalizedIndex]))
	if normalizedName == "" {
		return "", productsapi.CategoryBranch{}, nil
	}

	if _, err := strconv.Atoi(code); err != nil {
		return "", productsapi.CategoryBranch{}, fmt.Errorf("parse Code %q: %w", code, err)
	}

	return normalizedName, productsapi.CategoryBranch{
		Code: code,
		Name: name,
	}, nil
}

// firstNonEmpty devuelve el primer string no vacío después de trim.
func firstNonEmpty(values ...string) string {
	for _, value := range values {
		if strings.TrimSpace(value) != "" {
			return strings.TrimSpace(value)
		}
	}

	return ""
}

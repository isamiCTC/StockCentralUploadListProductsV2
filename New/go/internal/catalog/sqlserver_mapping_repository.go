package catalog

import (
	"context"
	"database/sql"
	"fmt"
	"strconv"
	"strings"
	"time"

	appconfig "stockcentraluploadlistproductsv2/internal/config"
	"stockcentraluploadlistproductsv2/internal/products"
	"stockcentraluploadlistproductsv2/internal/providers"
)

const globalProviderID = 0
const defaultCategoryMappingsSPName = "ProviderCategoryNameToRubroId_Get"

type SQLServerMappingRepository struct {
	server     *providers.SQLServer
	spName     string
	timeoutSec int
}

func NewSQLServerMappingRepository(server *providers.SQLServer, cfg appconfig.DatabaseConfig) *SQLServerMappingRepository {
	return &SQLServerMappingRepository{
		server:     server,
		spName:     firstNonEmpty(strings.TrimSpace(cfg.CategoryMappingsSPName), defaultCategoryMappingsSPName),
		timeoutSec: cfg.TimeoutSeconds,
	}
}

// LoadGlobalMappings trae el mapping común usado como primer intento
// de resolución para todas las filas del batch.
func (r *SQLServerMappingRepository) LoadGlobalMappings(ctx context.Context) (map[string]products.CategoryBranch, error) {
	queryCtx, cancel := context.WithTimeout(ctx, time.Duration(r.timeoutSec)*time.Second)
	defer cancel()

	rows, err := r.server.QueryContext(
		queryCtx,
		fmt.Sprintf("EXEC %s @ProviderId = @p1", r.spName),
		globalProviderID,
	)
	if err != nil {
		return nil, fmt.Errorf("execute category mappings stored procedure %s: %w", r.spName, err)
	}
	defer rows.Close()

	columns, err := rows.Columns()
	if err != nil {
		return nil, fmt.Errorf("read category mapping columns: %w", err)
	}

	columnIndex := make(map[string]int, len(columns))
	for i, column := range columns {
		columnIndex[strings.ToUpper(strings.TrimSpace(column))] = i
	}

	nameIndex, ok := columnIndex["PROVIDERCATEGORYNAME"]
	if !ok {
		return nil, fmt.Errorf("category mapping resultset does not include ProviderCategoryName column")
	}

	rubroIndex, ok := columnIndex["RUBROID"]
	if !ok {
		return nil, fmt.Errorf("category mapping resultset does not include RubroId column")
	}

	mappings := make(map[string]products.CategoryBranch)
	for rows.Next() {
		key, branch, scanErr := scanCategoryMappingRow(rows, nameIndex, rubroIndex, len(columns))
		if scanErr != nil {
			return nil, scanErr
		}
		if key == "" {
			continue
		}
		mappings[key] = branch
	}

	if err := rows.Err(); err != nil {
		return nil, fmt.Errorf("iterate category mapping rows: %w", err)
	}

	return mappings, nil
}

func scanCategoryMappingRow(rows *sql.Rows, nameIndex, rubroIndex, columnsCount int) (string, products.CategoryBranch, error) {
	rawValues := make([]sql.RawBytes, columnsCount)
	destinations := make([]any, columnsCount)
	for i := range rawValues {
		destinations[i] = &rawValues[i]
	}

	if err := rows.Scan(destinations...); err != nil {
		return "", products.CategoryBranch{}, fmt.Errorf("scan category mapping row: %w", err)
	}

	providerCategoryName := strings.TrimSpace(string(rawValues[nameIndex]))
	if providerCategoryName == "" {
		return "", products.CategoryBranch{}, nil
	}

	rubroID := strings.TrimSpace(string(rawValues[rubroIndex]))
	if rubroID == "" {
		return "", products.CategoryBranch{}, nil
	}

	if _, err := strconv.Atoi(rubroID); err != nil {
		return "", products.CategoryBranch{}, fmt.Errorf("parse RubroId %q: %w", rubroID, err)
	}

	return normalizeCategoryKey(providerCategoryName), products.CategoryBranch{
		Code: rubroID,
		Name: providerCategoryName,
	}, nil
}

func firstNonEmpty(values ...string) string {
	for _, value := range values {
		if strings.TrimSpace(value) != "" {
			return strings.TrimSpace(value)
		}
	}

	return ""
}

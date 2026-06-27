package sqlserver

import (
	"context"
	"database/sql"
	"fmt"
	"strconv"
	"strings"
	"time"

	appconfig "stockcentraluploadlistproductsv2/internal/config"
	"stockcentraluploadlistproductsv2/internal/providers"
)

// Este archivo implementa el repositorio real de providers contra SQL Server.
//
// La lógica replica la intención del legado:
// ejecutar el stored procedure `ProvidersGetListByEnabledAndIntegratorAndCatalogID`
// y transformar el resultset en una lista simple de providers válidos.
type ProvidersRepository struct {
	server          *SQLServer
	providersSPName string
}

// NewProvidersRepository recibe la conexión ya abierta y el nombre del SP
// desde configuración.
func NewProvidersRepository(server *SQLServer, cfg appconfig.DatabaseConfig) *ProvidersRepository {
	return &ProvidersRepository{
		server:          server,
		providersSPName: cfg.ProvidersSPName,
	}
}

// ListEnabledByIntegratorAndCatalog ejecuta el stored procedure legado.
//
// En vez de depender de posiciones fijas del resultset, busca las columnas
// `ID`, `Name` y opcionalmente `Email` por nombre. Eso hace la lectura un poco
// más robusta frente a cambios menores en el orden de columnas.
func (r *ProvidersRepository) ListEnabledByIntegratorAndCatalog(ctx context.Context, integratorID, catalogID int) ([]providers.Provider, error) {
	// Ejecutamos el SP de forma explícita para dejar visible qué parámetros
	// salen desde la aplicación.
	queryCtx, cancel := context.WithTimeout(ctx, time.Duration(r.server.timeoutSeconds)*time.Second)
	defer cancel()

	rows, err := r.server.QueryContext(
		queryCtx,
		fmt.Sprintf("EXEC %s @Enabled = @p1, @IntegratorID = @p2, @CatalogID = @p3", r.providersSPName),
		true,
		integratorID,
		catalogID,
	)
	if err != nil {
		return nil, fmt.Errorf("execute providers stored procedure %s: %w", r.providersSPName, err)
	}
	defer rows.Close()

	// Leemos primero los nombres de columnas para poder mapear el resultado
	// de forma explícita.
	columns, err := rows.Columns()
	if err != nil {
		return nil, fmt.Errorf("read provider columns: %w", err)
	}

	columnIndex := make(map[string]int, len(columns))
	for i, column := range columns {
		columnIndex[strings.ToUpper(strings.TrimSpace(column))] = i
	}

	idIndex, ok := columnIndex["ID"]
	if !ok {
		return nil, fmt.Errorf("provider resultset does not include ID column")
	}

	nameIndex, ok := columnIndex["NAME"]
	if !ok {
		return nil, fmt.Errorf("provider resultset does not include Name column")
	}

	// `Email` es opcional para no romper si en algún ambiente el SP no lo trae.
	// Si no existe, se trabaja con email vacío.
	emailIndex := -1
	if idx, ok := columnIndex["EMAIL"]; ok {
		emailIndex = idx
	}

	// Recorremos todas las filas y las convertimos al modelo mínimo del dominio.
	var providerList []providers.Provider
	for rows.Next() {
		// Cada fila del resultset representa un provider elegible.
		provider, scanErr := scanProviderRow(rows, idIndex, nameIndex, emailIndex, len(columns))
		if scanErr != nil {
			return nil, scanErr
		}
		providerList = append(providerList, provider)
	}

	if err := rows.Err(); err != nil {
		return nil, fmt.Errorf("iterate provider rows: %w", err)
	}

	return providerList, nil
}

// scanProviderRow toma una fila ya posicionada y extrae `ID`, `Name` y
// opcionalmente `Email`. El resto de columnas del SP se ignora.
func scanProviderRow(rows *sql.Rows, idIndex, nameIndex, emailIndex, columnsCount int) (providers.Provider, error) {
	rawValues := make([]sql.RawBytes, columnsCount)
	destinations := make([]any, columnsCount)
	for i := range rawValues {
		// Scan necesita recibir punteros; usamos un slice paralelo para no
		// asumir tipos concretos columna por columna.
		destinations[i] = &rawValues[i]
	}

	if err := rows.Scan(destinations...); err != nil {
		return providers.Provider{}, fmt.Errorf("scan provider row: %w", err)
	}

	providerID, err := strconv.Atoi(strings.TrimSpace(string(rawValues[idIndex])))
	if err != nil {
		return providers.Provider{}, fmt.Errorf("parse provider ID: %w", err)
	}

	providerEmail := ""
	if emailIndex >= 0 && emailIndex < len(rawValues) {
		providerEmail = strings.TrimSpace(string(rawValues[emailIndex]))
	}

	// El dominio de batch solo necesita ID, nombre y email.
	return providers.Provider{
		ID:    providerID,
		Name:  strings.TrimSpace(string(rawValues[nameIndex])),
		Email: providerEmail,
	}, nil
}

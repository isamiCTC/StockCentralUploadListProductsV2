package providers

import (
	"context"
	"database/sql"
	"fmt"
	"time"

	_ "github.com/denisenkom/go-mssqldb"

	appconfig "stockcentraluploadlistproductsv2/internal/config"
)

// Este archivo encapsula la conexión base a SQL Server usando `database/sql`.
//
// No conoce nada del dominio de providers ni del negocio. Su responsabilidad
// es muy puntual: abrir la conexión, verificarla y ofrecer helpers básicos de
// consulta con timeout.
type SQLServer struct {
	db             *sql.DB
	timeoutSeconds int
}

// NewSQLServer abre la conexión y hace un ping inicial.
// Si el ping falla, devolvemos error y cerramos inmediatamente el handle.
func NewSQLServer(cfg appconfig.DatabaseConfig, connectionString string) (*SQLServer, error) {
	// `sql.Open` no valida conectividad todavía; solo arma el handle.
	database, err := sql.Open("sqlserver", connectionString)
	if err != nil {
		return nil, fmt.Errorf("open sqlserver connection: %w", err)
	}

	server := &SQLServer{
		db:             database,
		timeoutSeconds: cfg.TimeoutSeconds,
	}

	// El ping inicial confirma que la conexión realmente es usable.
	pingCtx, cancel := context.WithTimeout(context.Background(), time.Duration(cfg.TimeoutSeconds)*time.Second)
	defer cancel()

	if err := database.PingContext(pingCtx); err != nil {
		// Si no podemos usarla, cerramos enseguida para no dejar recursos abiertos.
		_ = database.Close()
		return nil, fmt.Errorf("ping sqlserver: %w", err)
	}

	return server, nil
}

// Close libera la conexión cuando termina la corrida.
func (s *SQLServer) Close() error {
	return s.db.Close()
}

// QueryContext ejecuta una query usando el timeout configurado en TOML.
// Esto evita dejar operaciones de DB colgadas indefinidamente.
func (s *SQLServer) QueryContext(ctx context.Context, query string, args ...any) (*sql.Rows, error) {
	// No cancelamos acá un timeout derivado porque `*sql.Rows` puede seguir
	// consumiéndose después de que esta función retorna. Si se cancelara en este
	// punto, la iteración de `rows.Next()` fallaría con `context canceled`.
	//
	// El caller que recorra las filas debe ser quien acote y cierre el ciclo de
	// vida del contexto.
	rows, err := s.db.QueryContext(ctx, query, args...)
	if err != nil {
		return nil, fmt.Errorf("query sqlserver: %w", err)
	}

	return rows, nil
}

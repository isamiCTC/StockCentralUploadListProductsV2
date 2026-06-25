package providers

import (
	"context"
	"database/sql"
	"database/sql/driver"
	"errors"
	"io"
	"sync"
	"testing"
)

func TestQueryContextKeepsRowsUsableAfterReturn(t *testing.T) {
	t.Parallel()

	driverName := registerContextAwareDriver()

	db, err := sql.Open(driverName, "")
	if err != nil {
		t.Fatalf("open db: %v", err)
	}
	t.Cleanup(func() {
		_ = db.Close()
	})

	server := &SQLServer{
		db:             db,
		timeoutSeconds: 1,
	}

	rows, err := server.QueryContext(context.Background(), "test-query")
	if err != nil {
		t.Fatalf("query context: %v", err)
	}
	t.Cleanup(func() {
		_ = rows.Close()
	})

	if !rows.Next() {
		if err := rows.Err(); err != nil {
			t.Fatalf("expected rows to remain usable after QueryContext returned, got error: %v", err)
		}
		t.Fatal("expected one row")
	}
}

var (
	registerDriverOnce sync.Once
	registeredDriver   = "context-aware-test-driver"
)

func registerContextAwareDriver() string {
	registerDriverOnce.Do(func() {
		sql.Register(registeredDriver, contextAwareDriver{})
	})
	return registeredDriver
}

type contextAwareDriver struct{}

func (d contextAwareDriver) Open(name string) (driver.Conn, error) {
	return contextAwareConn{}, nil
}

type contextAwareConn struct{}

func (c contextAwareConn) Prepare(query string) (driver.Stmt, error) {
	return nil, errors.New("not implemented")
}

func (c contextAwareConn) Close() error {
	return nil
}

func (c contextAwareConn) Begin() (driver.Tx, error) {
	return nil, errors.New("not implemented")
}

func (c contextAwareConn) QueryContext(ctx context.Context, query string, args []driver.NamedValue) (driver.Rows, error) {
	return &contextAwareRows{
		ctx:  ctx,
		sent: false,
	}, nil
}

type contextAwareRows struct {
	ctx  context.Context
	sent bool
}

func (r *contextAwareRows) Columns() []string {
	return []string{"ID"}
}

func (r *contextAwareRows) Close() error {
	return nil
}

func (r *contextAwareRows) Next(dest []driver.Value) error {
	if err := r.ctx.Err(); err != nil {
		return err
	}
	if r.sent {
		return io.EOF
	}
	dest[0] = "123"
	r.sent = true
	return nil
}

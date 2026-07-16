package productsapi

import (
	"context"
	"net/http"
	"strings"
	"testing"

	appconfig "stockcentraluploadlistproductsv2/internal/config"
)

// Este archivo prueba el upsert legado de productos contra un servidor HTTP fake.

func TestUpsertProductLegacyCreatesWhenProductDoesNotExist(t *testing.T) {
	t.Parallel()

	calls := newCallCounter()
	client := NewClient(appconfig.ProductsAPIConfig{
		BaseURL:        "http://example.test",
		ProviderName:   "CTC",
		TimeoutSeconds: 5,
	}, "token")
	client.http.SetTransport(roundTripFunc(func(r *http.Request) (*http.Response, error) {
		switch r.Method {
		case http.MethodPut:
			calls.inc("PUT")
			return jsonResponse(http.StatusBadRequest, `{"Result":{"Description":"Producto inexistente"}}`, r), nil
		case http.MethodPost:
			calls.inc("POST")
			return jsonResponse(http.StatusOK, `{}`, r), nil
		default:
			return jsonResponse(http.StatusMethodNotAllowed, `{}`, r), nil
		}
	}))

	result, err := client.UpsertProductLegacy(context.Background(), 342, Product{Sku: "ABC123"})
	if err != nil {
		t.Fatalf("UpsertProductLegacy returned error: %v", err)
	}
	if result.Action != "CREATE" {
		t.Fatalf("Action = %q, want CREATE", result.Action)
	}
	if calls.get("PUT") != 1 || calls.get("POST") != 1 {
		t.Fatalf("PUT calls = %d, POST calls = %d, want 1 and 1", calls.get("PUT"), calls.get("POST"))
	}
}

func TestUpsertProductLegacyFailsWhenCreateReturnsNon2xx(t *testing.T) {
	t.Parallel()

	client := NewClient(appconfig.ProductsAPIConfig{
		BaseURL:        "http://example.test",
		ProviderName:   "CTC",
		TimeoutSeconds: 5,
	}, "token")
	client.http.SetTransport(roundTripFunc(func(r *http.Request) (*http.Response, error) {
		switch r.Method {
		case http.MethodPut:
			return jsonResponse(http.StatusBadRequest, `{"Result":{"Description":"Producto inexistente"}}`, r), nil
		case http.MethodPost:
			return jsonResponse(http.StatusInternalServerError, `{"error":"boom"}`, r), nil
		default:
			return jsonResponse(http.StatusMethodNotAllowed, `{}`, r), nil
		}
	}))

	if _, err := client.UpsertProductLegacy(context.Background(), 342, Product{Sku: "ABC123"}); err == nil {
		t.Fatal("UpsertProductLegacy should fail when create returns non-2xx")
	} else if !strings.Contains(err.Error(), `body="{\"error\":\"boom\"}"`) {
		t.Fatalf("error = %q, want response body included", err.Error())
	}
}

func TestUpsertProductLegacyFailsWhenUpdateReturnsNon2xxAndIncludesBody(t *testing.T) {
	t.Parallel()

	client := NewClient(appconfig.ProductsAPIConfig{
		BaseURL:        "http://example.test",
		ProviderName:   "CTC",
		TimeoutSeconds: 5,
	}, "token")
	client.http.SetTransport(roundTripFunc(func(r *http.Request) (*http.Response, error) {
		switch r.Method {
		case http.MethodPut:
			return jsonResponse(http.StatusBadRequest, `{"Message":"validation failed"}`, r), nil
		default:
			return jsonResponse(http.StatusMethodNotAllowed, `{}`, r), nil
		}
	}))

	if _, err := client.UpsertProductLegacy(context.Background(), 342, Product{Sku: "ABC123"}); err == nil {
		t.Fatal("UpsertProductLegacy should fail when update returns non-2xx")
	} else if !strings.Contains(err.Error(), `body="{\"Message\":\"validation failed\"}"`) {
		t.Fatalf("error = %q, want response body included", err.Error())
	}
}

func TestUpsertProductLegacyRetriesUpdateWhenAPIReportsDeadlock(t *testing.T) {
	t.Parallel()

	calls := newCallCounter()
	client := newDeadlockRetryTestClient()
	client.http.SetTransport(roundTripFunc(func(r *http.Request) (*http.Response, error) {
		calls.inc("PUT")
		if calls.get("PUT") == 1 {
			return jsonResponse(http.StatusInternalServerError, deadlockResponseBody, r), nil
		}
		return jsonResponse(http.StatusOK, `{}`, r), nil
	}))

	result, err := client.UpsertProductLegacy(context.Background(), 456, Product{Sku: "21087"})
	if err != nil {
		t.Fatalf("UpsertProductLegacy returned error: %v", err)
	}
	if result.Action != "UPDATE" || result.UpdateAttempts != 2 {
		t.Fatalf("result = %+v, want UPDATE with 2 attempts", result)
	}
	if calls.get("PUT") != 2 {
		t.Fatalf("PUT calls = %d, want 2", calls.get("PUT"))
	}
}

func TestUpsertProductLegacyRetriesCreateWhenAPIReportsDeadlock(t *testing.T) {
	t.Parallel()

	calls := newCallCounter()
	client := newDeadlockRetryTestClient()
	client.http.SetTransport(roundTripFunc(func(r *http.Request) (*http.Response, error) {
		switch r.Method {
		case http.MethodPut:
			calls.inc("PUT")
			return jsonResponse(http.StatusBadRequest, `{"Result":{"Description":"Producto inexistente"}}`, r), nil
		case http.MethodPost:
			calls.inc("POST")
			if calls.get("POST") == 1 {
				return jsonResponse(http.StatusInternalServerError, deadlockResponseBody, r), nil
			}
			return jsonResponse(http.StatusOK, `{}`, r), nil
		default:
			return jsonResponse(http.StatusMethodNotAllowed, `{}`, r), nil
		}
	}))

	result, err := client.UpsertProductLegacy(context.Background(), 456, Product{Sku: "21087"})
	if err != nil {
		t.Fatalf("UpsertProductLegacy returned error: %v", err)
	}
	if result.Action != "CREATE" || result.UpdateAttempts != 1 || result.CreateAttempts != 2 {
		t.Fatalf("result = %+v, want CREATE with 1 update and 2 create attempts", result)
	}
	if calls.get("PUT") != 1 || calls.get("POST") != 2 {
		t.Fatalf("PUT calls = %d, POST calls = %d, want 1 and 2", calls.get("PUT"), calls.get("POST"))
	}
}

func TestUpsertProductLegacyDoesNotRetryUnrelatedInternalServerError(t *testing.T) {
	t.Parallel()

	calls := newCallCounter()
	client := newDeadlockRetryTestClient()
	client.http.SetTransport(roundTripFunc(func(r *http.Request) (*http.Response, error) {
		calls.inc("PUT")
		return jsonResponse(http.StatusInternalServerError, `{"Result":{"Description":"Error inesperado"}}`, r), nil
	}))

	if _, err := client.UpsertProductLegacy(context.Background(), 456, Product{Sku: "21087"}); err == nil {
		t.Fatal("UpsertProductLegacy should fail for an unrelated HTTP 500")
	}
	if calls.get("PUT") != 1 {
		t.Fatalf("PUT calls = %d, want 1", calls.get("PUT"))
	}
}

func TestUpsertProductLegacyStopsAfterConfiguredDeadlockAttempts(t *testing.T) {
	t.Parallel()

	calls := newCallCounter()
	client := newDeadlockRetryTestClient()
	client.http.SetTransport(roundTripFunc(func(r *http.Request) (*http.Response, error) {
		calls.inc("PUT")
		return jsonResponse(http.StatusInternalServerError, deadlockResponseBody, r), nil
	}))

	if _, err := client.UpsertProductLegacy(context.Background(), 456, Product{Sku: "21087"}); err == nil {
		t.Fatal("UpsertProductLegacy should fail after exhausting deadlock retries")
	}
	if calls.get("PUT") != 3 {
		t.Fatalf("PUT calls = %d, want 3", calls.get("PUT"))
	}
}

func newDeadlockRetryTestClient() *Client {
	return NewClient(appconfig.ProductsAPIConfig{
		BaseURL:                 "http://example.test",
		ProviderName:            "CTC",
		TimeoutSeconds:          5,
		DeadlockMaxAttempts:     3,
		DeadlockBaseDelayMillis: 1,
	}, "token")
}

const deadlockResponseBody = `{"Success":false,"Result":{"Description":"Error al intentar realizar la actualización del producto. [Exception]\r\nMessage = La transacción (id. de proceso 2330) quedó en interbloqueo en bloqueo | búfer de comunicaciones recursos con otro proceso y fue elegida como sujeto del interbloqueo. Ejecute de nuevo la transacción."}}`

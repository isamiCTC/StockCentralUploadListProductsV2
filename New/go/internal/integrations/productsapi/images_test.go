package productsapi

import (
	"context"
	"net/http"
	"strings"
	"testing"

	appconfig "stockcentraluploadlistproductsv2/internal/config"
)

// Este archivo prueba la semántica legacy de sincronización de imágenes.

func TestSyncImageLegacySkipsWhenBase64IsEqual(t *testing.T) {
	t.Parallel()

	client := NewClient(appconfig.ProductsAPIConfig{
		BaseURL:        "http://example.test",
		ProviderName:   "CTC",
		TimeoutSeconds: 5,
	}, "token")
	client.http.SetTransport(roundTripFunc(func(r *http.Request) (*http.Response, error) {
		if r.Method != http.MethodGet {
			t.Fatalf("unexpected method %s", r.Method)
		}
		return jsonResponse(http.StatusOK, `{"Result":{"Base64":"same-base64"}}`, r), nil
	}))

	result, err := client.SyncImageLegacy(context.Background(), 342, "ABC123", 0, "same-base64")
	if err != nil {
		t.Fatalf("SyncImageLegacy returned error: %v", err)
	}
	if result.Action != "SKIP_SAME_IMAGE" {
		t.Fatalf("Action = %q, want SKIP_SAME_IMAGE", result.Action)
	}
}

func TestSyncImageLegacyCreatesWhenPutSaysImageNotFound(t *testing.T) {
	t.Parallel()

	calls := newCallCounter()
	client := NewClient(appconfig.ProductsAPIConfig{
		BaseURL:        "http://example.test",
		ProviderName:   "CTC",
		TimeoutSeconds: 5,
	}, "token")
	client.http.SetTransport(roundTripFunc(func(r *http.Request) (*http.Response, error) {
		switch r.Method {
		case http.MethodGet:
			return jsonResponse(http.StatusNotFound, `{}`, r), nil
		case http.MethodPut:
			calls.inc("PUT")
			return jsonResponse(http.StatusBadRequest, `{"TransactionId":"34|Imagen inexistente"}`, r), nil
		case http.MethodPost:
			calls.inc("POST")
			return jsonResponse(http.StatusOK, `{}`, r), nil
		default:
			return jsonResponse(http.StatusMethodNotAllowed, `{}`, r), nil
		}
	}))

	result, err := client.SyncImageLegacy(context.Background(), 342, "ABC123", 0, "new-base64")
	if err != nil {
		t.Fatalf("SyncImageLegacy returned error: %v", err)
	}
	if result.Action != "CREATE" {
		t.Fatalf("Action = %q, want CREATE", result.Action)
	}
	if calls.get("PUT") != 1 || calls.get("POST") != 1 {
		t.Fatalf("PUT calls = %d, POST calls = %d, want 1 and 1", calls.get("PUT"), calls.get("POST"))
	}
}

func TestSyncImageLegacyFailsWhenUpdateReturnsNon2xx(t *testing.T) {
	t.Parallel()

	client := NewClient(appconfig.ProductsAPIConfig{
		BaseURL:        "http://example.test",
		ProviderName:   "CTC",
		TimeoutSeconds: 5,
	}, "token")
	client.http.SetTransport(roundTripFunc(func(r *http.Request) (*http.Response, error) {
		switch r.Method {
		case http.MethodGet:
			return jsonResponse(http.StatusNotFound, `{}`, r), nil
		case http.MethodPut:
			return jsonResponse(http.StatusInternalServerError, `{"error":"boom"}`, r), nil
		default:
			return jsonResponse(http.StatusMethodNotAllowed, `{}`, r), nil
		}
	}))

	if _, err := client.SyncImageLegacy(context.Background(), 342, "ABC123", 0, "new-base64"); err == nil {
		t.Fatal("SyncImageLegacy should fail when update returns non-2xx")
	} else if !strings.Contains(err.Error(), `body="{\"error\":\"boom\"}"`) {
		t.Fatalf("error = %q, want response body included", err.Error())
	}
}

func TestSyncImageLegacyFailsWhenCreateReturnsNon2xxAndIncludesBody(t *testing.T) {
	t.Parallel()

	client := NewClient(appconfig.ProductsAPIConfig{
		BaseURL:        "http://example.test",
		ProviderName:   "CTC",
		TimeoutSeconds: 5,
	}, "token")
	client.http.SetTransport(roundTripFunc(func(r *http.Request) (*http.Response, error) {
		switch r.Method {
		case http.MethodGet:
			return jsonResponse(http.StatusNotFound, `{}`, r), nil
		case http.MethodPut:
			return jsonResponse(http.StatusBadRequest, `{"TransactionId":"34|Imagen inexistente"}`, r), nil
		case http.MethodPost:
			return jsonResponse(http.StatusInternalServerError, `{"Success":false,"Message":"boom"}`, r), nil
		default:
			return jsonResponse(http.StatusMethodNotAllowed, `{}`, r), nil
		}
	}))

	if _, err := client.SyncImageLegacy(context.Background(), 342, "ABC123", 0, "new-base64"); err == nil {
		t.Fatal("SyncImageLegacy should fail when create returns non-2xx")
	} else if !strings.Contains(err.Error(), `body="{\"Success\":false,\"Message\":\"boom\"}"`) {
		t.Fatalf("error = %q, want response body included", err.Error())
	}
}

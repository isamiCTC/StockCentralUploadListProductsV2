package catalog

import (
	"context"
	"net/http"
	"net/http/httptest"
	"testing"

	appconfig "stockcentraluploadlistproductsv2/internal/config"
	"stockcentraluploadlistproductsv2/internal/products"
)

// Este archivo prueba la resolución de categorías y subcategorías.
//
// La idea es blindar el orden de decisión acordado:
// - primero mapping hardcodeado;
// - después fallback a API;
// - y finalmente ramas de respaldo cuando no hay match.

func TestResolveBySubcategoryMatchesHardcodedWithLooseNormalization(t *testing.T) {
	t.Parallel()

	server := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		t.Fatalf("hardcoded resolution should not call API, got %s %s", r.Method, r.URL.Path)
	}))
	defer server.Close()

	client := products.NewClient(appconfig.ProductsAPIConfig{
		BaseURL:        server.URL,
		ProviderName:   "CTC",
		TimeoutSeconds: 5,
	}, "token")

	resolver := NewResolver(client)
	result, err := resolver.ResolveBySubcategory(context.Background(), 342, "  pequeños   electrodomésticos  ")
	if err != nil {
		t.Fatalf("ResolveBySubcategory returned error: %v", err)
	}
	if result.Source != ResolutionSourceHardcoded {
		t.Fatalf("Source = %q, want %q", result.Source, ResolutionSourceHardcoded)
	}
	if result.Branch.Code != "1212" {
		t.Fatalf("Branch.Code = %q, want %q", result.Branch.Code, "1212")
	}
}

func TestResolveBySubcategoryCallsAPIWithOriginalValueWhenHardcodedDoesNotMatch(t *testing.T) {
	t.Parallel()

	original := " Sub Categoría Nueva "

	server := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		if got := r.URL.EscapedPath(); got != "/Mp_ProductsAPI_CTC/subcategories/342/%20Sub%20Categor%C3%ADa%20Nueva%20" {
			t.Fatalf("EscapedPath = %q, want original value in path", got)
		}
		w.Header().Set("Content-Type", "application/json")
		_, _ = w.Write([]byte(`[{"ID":"555","Name":"Desde API"}]`))
	}))
	defer server.Close()

	client := products.NewClient(appconfig.ProductsAPIConfig{
		BaseURL:        server.URL,
		ProviderName:   "CTC",
		TimeoutSeconds: 5,
	}, "token")

	resolver := NewResolver(client)
	result, err := resolver.ResolveBySubcategory(context.Background(), 342, original)
	if err != nil {
		t.Fatalf("ResolveBySubcategory returned error: %v", err)
	}
	if result.Source != ResolutionSourceAPI {
		t.Fatalf("Source = %q, want %q", result.Source, ResolutionSourceAPI)
	}
	if result.Branch.Code != "555" {
		t.Fatalf("Branch.Code = %q, want %q", result.Branch.Code, "555")
	}
}

func TestResolveBySubcategoryFallsBackWhenAPIHasNoMatches(t *testing.T) {
	t.Parallel()

	server := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "application/json")
		_, _ = w.Write([]byte(`[]`))
	}))
	defer server.Close()

	client := products.NewClient(appconfig.ProductsAPIConfig{
		BaseURL:        server.URL,
		ProviderName:   "CTC",
		TimeoutSeconds: 5,
	}, "token")

	resolver := NewResolver(client)
	result, err := resolver.ResolveBySubcategory(context.Background(), 342, "Subcategoria sin match")
	if err != nil {
		t.Fatalf("ResolveBySubcategory returned error: %v", err)
	}
	if result.Source != ResolutionSourceFallback {
		t.Fatalf("Source = %q, want %q", result.Source, ResolutionSourceFallback)
	}
	if result.Branch.Code != "1041" {
		t.Fatalf("Branch.Code = %q, want %q", result.Branch.Code, "1041")
	}
}

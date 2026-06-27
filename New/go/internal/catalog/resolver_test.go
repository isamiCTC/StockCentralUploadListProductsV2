package catalog

import (
	"context"
	"testing"

	productsapi "stockcentraluploadlistproductsv2/internal/integrations/productsapi"
)

// Este archivo prueba la resolución de categorías y subcategorías.
//
// La idea es blindar el orden de decisión acordado:
// - primero catálogo precargado desde DB;
// - y después rama de respaldo cuando no hay match.

func TestResolveBySubcategoryMatchesDatabaseMappingWithLooseNormalization(t *testing.T) {
	t.Parallel()

	fallback := productsapi.CategoryBranch{Code: "1041", Name: "Varios"}
	resolver := NewResolver(map[string]productsapi.CategoryBranch{
		NormalizeCategoryKey("PEQUEÑOS ELECTRODOMESTICOS"): {Code: "1212", Name: "Pequeños Electro"},
	}, fallback)
	result, err := resolver.ResolveBySubcategory(context.Background(), 342, "  pequeños   electrodomésticos  ")
	if err != nil {
		t.Fatalf("ResolveBySubcategory returned error: %v", err)
	}
	if result.Source != ResolutionSourceDatabase {
		t.Fatalf("Source = %q, want %q", result.Source, ResolutionSourceDatabase)
	}
	if result.Branch.Code != "1212" {
		t.Fatalf("Branch.Code = %q, want %q", result.Branch.Code, "1212")
	}
}

func TestResolveBySubcategoryFallsBackWhenDatabaseMappingDoesNotMatch(t *testing.T) {
	t.Parallel()

	resolver := NewResolver(nil, productsapi.CategoryBranch{Code: "1041", Name: "Varios"})
	result, err := resolver.ResolveBySubcategory(context.Background(), 342, " Sub Categoría Nueva ")
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

func TestResolveBySubcategoryFallsBackWhenDatabaseMappingHasNoMatches(t *testing.T) {
	t.Parallel()

	resolver := NewResolver(nil, productsapi.CategoryBranch{Code: "1041", Name: "Varios"})
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

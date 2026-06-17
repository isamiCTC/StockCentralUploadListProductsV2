package catalog

import (
	"context"

	"stockcentraluploadlistproductsv2/internal/products"
)

// Este archivo implementa la resolución de categoría/subcategoría.
//
// Orden respetado del legacy:
// 1. usar el mapping hardcodeado
// 2. si no matchea, consultar la API de subcategorías
// 3. si tampoco resuelve, usar "Varios" (1041)

type ResolutionSource string

const (
	ResolutionSourceHardcoded ResolutionSource = "HARDCODED"
	ResolutionSourceAPI       ResolutionSource = "API"
	ResolutionSourceFallback  ResolutionSource = "FALLBACK"
)

type ResolutionResult struct {
	Branch products.CategoryBranch
	Source ResolutionSource
}

type Resolver struct {
	client *products.Client
}

// NewResolver construye el resolvedor de categorías.
func NewResolver(client *products.Client) *Resolver {
	return &Resolver{client: client}
}

// ResolveBySubcategory aplica la cadena completa de resolución.
func (r *Resolver) ResolveBySubcategory(ctx context.Context, providerID int, subcategory string) (ResolutionResult, error) {
	// Primer intento: mapping propio del proyecto, sin salir a la red.
	if branch, ok := hardcodedBranches[normalizeCategoryKey(subcategory)]; ok {
		return ResolutionResult{
			Branch: branch,
			Source: ResolutionSourceHardcoded,
		}, nil
	}

	// Segundo intento: preguntar a la API por la primera coincidencia útil.
	branch, _, err := r.client.ResolveFirstSubcategory(ctx, providerID, subcategory)
	if err == nil && branch != nil {
		return ResolutionResult{
			Branch: *branch,
			Source: ResolutionSourceAPI,
		}, nil
	}

	// Último recurso: mandar todo a la categoría comodín "Varios".
	return ResolutionResult{
		Branch: fallbackBranch,
		Source: ResolutionSourceFallback,
	}, nil
}

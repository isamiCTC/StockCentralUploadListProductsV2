package catalog

import (
	"context"

	productsapi "stockcentraluploadlistproductsv2/internal/integrations/productsapi"
)

// Este archivo implementa la resolución de categoría/subcategoría.
//
// Regla vigente:
// 1. usar el catálogo precargado desde SQL Server;
// 2. si no matchea, usar la categoría general configurada.

type ResolutionSource string

const (
	ResolutionSourceDatabase ResolutionSource = "DATABASE"
	ResolutionSourceFallback ResolutionSource = "FALLBACK"
)

type ResolutionResult struct {
	Branch productsapi.CategoryBranch
	Source ResolutionSource
}

type Resolver struct {
	branchByKey map[string]productsapi.CategoryBranch
	fallback    productsapi.CategoryBranch
}

// NewResolver construye el resolvedor de categorías.
func NewResolver(branchByKey map[string]productsapi.CategoryBranch, fallback productsapi.CategoryBranch) *Resolver {
	if branchByKey == nil {
		branchByKey = make(map[string]productsapi.CategoryBranch)
	}

	return &Resolver{
		branchByKey: branchByKey,
		fallback:    fallback,
	}
}

// ResolveBySubcategory resuelve contra el catálogo precargado y, si no
// encuentra match, cae a la categoría general configurada.
func (r *Resolver) ResolveBySubcategory(ctx context.Context, providerID int, subcategory string) (ResolutionResult, error) {
	_ = ctx
	_ = providerID

	if branch, ok := r.branchByKey[NormalizeCategoryKey(subcategory)]; ok {
		return ResolutionResult{
			Branch: branch,
			Source: ResolutionSourceDatabase,
		}, nil
	}

	return ResolutionResult{
		Branch: r.fallback,
		Source: ResolutionSourceFallback,
	}, nil
}

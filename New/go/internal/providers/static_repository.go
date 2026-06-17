package providers

import (
	"context"

	appconfig "stockcentraluploadlistproductsv2/internal/config"
	"stockcentraluploadlistproductsv2/internal/domain"
)

// StaticRepository es un fallback simple usado durante etapas tempranas
// de bootstrap o tests.
//
// Hoy devuelve vacío porque preferimos no inventar providers si la integración
// real con SQL no está conectada.
type StaticRepository struct {
	cfg appconfig.BatchConfig
}

// NewStaticRepository construye el repositorio estático.
func NewStaticRepository(cfg appconfig.BatchConfig) *StaticRepository {
	return &StaticRepository{cfg: cfg}
}

// ListEnabledByIntegratorAndCatalog devuelve un conjunto vacío a propósito.
// Eso evita escanear carpetas incorrectas cuando no hay fuente real de datos.
func (r *StaticRepository) ListEnabledByIntegratorAndCatalog(_ context.Context, _ int, _ int) ([]domain.Provider, error) {
	_ = r.cfg
	return []domain.Provider{}, nil
}

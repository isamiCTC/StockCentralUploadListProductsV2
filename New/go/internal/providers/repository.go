package providers

import (
	"context"

	"stockcentraluploadlistproductsv2/internal/domain"
)

// ProviderRepository define el contrato que necesita el batch para obtener
// providers habilitados.
//
// El batch no debería saber si vienen de SQL Server, un mock o cualquier otra
// fuente. Esa es justamente la ventaja de esta interfaz.
type ProviderRepository interface {
	ListEnabledByIntegratorAndCatalog(ctx context.Context, integratorID, catalogID int) ([]domain.Provider, error)
}

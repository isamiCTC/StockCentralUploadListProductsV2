package providers

import (
	"context"
)

// Este archivo declara el contrato de acceso a providers para el batch.
//
// Su responsabilidad es aislar al orquestador de la fuente concreta de datos.
// Así el resto del proceso solo habla en términos de providers habilitados,
// sin depender de SQL Server, mocks o cualquier otra implementación.
type ProviderRepository interface {
	ListEnabledByIntegratorAndCatalog(ctx context.Context, integratorID, catalogID int) ([]Provider, error)
}

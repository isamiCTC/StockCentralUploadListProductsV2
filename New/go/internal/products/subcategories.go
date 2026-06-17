package products

import (
	"context"
	"encoding/json"
	"fmt"
	"net/http"
)

// Este archivo implementa la consulta de subcategorías usada como fallback
// por el legado cuando el mapeo hardcodeado no resuelve una categoría.

// GetSubcategories consulta la API de subcategorías por provider y texto.
func (c *Client) GetSubcategories(ctx context.Context, providerID int, subcategoryName string) ([]Subcategory, *restyMeta, error) {
	// La búsqueda se hace por texto y provider, tal como espera la API.
	response, err := c.newRequest(ctx).Get(c.subcategoriesPath(providerID, subcategoryName))
	if err != nil {
		return nil, nil, fmt.Errorf("get subcategories for %q: %w", subcategoryName, err)
	}

	meta := newRestyMeta(response)
	// Solo un 200 OK se considera respuesta utilizable.
	if response.StatusCode() != http.StatusOK {
		return nil, meta, fmt.Errorf("get subcategories for %q returned status %d", subcategoryName, response.StatusCode())
	}

	// La API devuelve una lista simple, no un envelope.
	var subcategories []Subcategory
	if err := json.Unmarshal(response.Body(), &subcategories); err != nil {
		return nil, meta, fmt.Errorf("decode subcategories for %q: %w", subcategoryName, err)
	}

	return subcategories, meta, nil
}

// ResolveFirstSubcategory devuelve la primera coincidencia, replicando la
// semántica práctica observada en el código legacy.
func (c *Client) ResolveFirstSubcategory(ctx context.Context, providerID int, subcategoryName string) (*CategoryBranch, *restyMeta, error) {
	// Reutilizamos la consulta base y después aplicamos la regla práctica
	// del legado: quedarse con la primera coincidencia.
	subcategories, meta, err := c.GetSubcategories(ctx, providerID, subcategoryName)
	if err != nil {
		return nil, meta, err
	}
	if len(subcategories) == 0 {
		// No es error técnico: simplemente no hubo match.
		return nil, meta, nil
	}

	// Convertimos la subcategoría encontrada a la rama mínima que usa el batch.
	return &CategoryBranch{
		Code: subcategories[0].ID,
		Name: subcategories[0].Name,
	}, meta, nil
}

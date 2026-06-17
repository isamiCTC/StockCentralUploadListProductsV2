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
	response, err := c.newRequest(ctx).Get(c.subcategoriesPath(providerID, subcategoryName))
	if err != nil {
		return nil, nil, fmt.Errorf("get subcategories for %q: %w", subcategoryName, err)
	}

	meta := newRestyMeta(response)
	if response.StatusCode() != http.StatusOK {
		return nil, meta, fmt.Errorf("get subcategories for %q returned status %d", subcategoryName, response.StatusCode())
	}

	var subcategories []Subcategory
	if err := json.Unmarshal(response.Body(), &subcategories); err != nil {
		return nil, meta, fmt.Errorf("decode subcategories for %q: %w", subcategoryName, err)
	}

	return subcategories, meta, nil
}

// ResolveFirstSubcategory devuelve la primera coincidencia, replicando la
// semántica práctica observada en el código legacy.
func (c *Client) ResolveFirstSubcategory(ctx context.Context, providerID int, subcategoryName string) (*CategoryBranch, *restyMeta, error) {
	subcategories, meta, err := c.GetSubcategories(ctx, providerID, subcategoryName)
	if err != nil {
		return nil, meta, err
	}
	if len(subcategories) == 0 {
		return nil, meta, nil
	}

	return &CategoryBranch{
		Code: subcategories[0].ID,
		Name: subcategories[0].Name,
	}, meta, nil
}

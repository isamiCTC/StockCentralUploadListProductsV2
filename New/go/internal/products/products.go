package products

import (
	"context"
	"encoding/json"
	"fmt"
	"net/http"
	"strings"
)

// Este archivo implementa las operaciones de productos respetando la
// semántica observada en el legacy.
//
// Regla clave:
// - primero intenta `PUT`
// - si la API responde BadRequest con `Producto inexistente`
// - entonces intenta `POST`

// GetProduct obtiene un producto existente por SKU.
func (c *Client) GetProduct(ctx context.Context, providerID int, sku string) (Product, *restyMeta, error) {
	response, err := c.newRequest(ctx).Get(c.providerProductPath(providerID, sku))
	if err != nil {
		return Product{}, nil, fmt.Errorf("get product %s: %w", sku, err)
	}

	meta := newRestyMeta(response)
	if response.StatusCode() != http.StatusOK {
		return Product{}, meta, fmt.Errorf("get product %s returned status %d", sku, response.StatusCode())
	}

	var envelope ProductEnvelope
	if err := json.Unmarshal(response.Body(), &envelope); err != nil {
		return Product{}, meta, fmt.Errorf("decode product envelope for %s: %w", sku, err)
	}

	return envelope.Result, meta, nil
}

// UpdateProduct envía el payload completo por PUT.
func (c *Client) UpdateProduct(ctx context.Context, providerID int, sku string, product Product) (*restyMeta, error) {
	response, err := c.newRequest(ctx).SetBody(product).Put(c.providerProductPath(providerID, sku))
	if err != nil {
		return nil, fmt.Errorf("update product %s: %w", sku, err)
	}

	return newRestyMeta(response), nil
}

// CreateProduct crea un producto nuevo por POST.
func (c *Client) CreateProduct(ctx context.Context, providerID int, product Product) (*restyMeta, error) {
	response, err := c.newRequest(ctx).SetBody(product).Post(c.providerProductsPath(providerID))
	if err != nil {
		return nil, fmt.Errorf("create product %s: %w", product.Sku, err)
	}

	return newRestyMeta(response), nil
}

// UpsertProductLegacy implementa el patrón exacto del servicio original:
// intentar update y, si la API dice "Producto inexistente", pasar a create.
func (c *Client) UpsertProductLegacy(ctx context.Context, providerID int, product Product) (UpsertResult, error) {
	// El primer intento siempre es update, igual que en el proceso original.
	updateMeta, err := c.UpdateProduct(ctx, providerID, product.Sku, product)
	if err != nil {
		return UpsertResult{}, err
	}

	// Si la API responde "producto inexistente", cambiamos de estrategia
	// y lo intentamos crear.
	if updateMeta.StatusCode == http.StatusBadRequest && isProductNotFound(updateMeta.Body) {
		createMeta, createErr := c.CreateProduct(ctx, providerID, product)
		if createErr != nil {
			return UpsertResult{}, createErr
		}
		if !isSuccessfulStatus(createMeta.StatusCode) {
			return UpsertResult{}, formatHTTPFailure(
				fmt.Sprintf("create product %s", product.Sku),
				createMeta.StatusCode,
				createMeta.Body,
			)
		}

		return UpsertResult{
			Action:     "CREATE",
			UpdateMeta: updateMeta,
			CreateMeta: createMeta,
		}, nil
	}

	// Si no hubo fallback y el update no fue exitoso, devolvemos error.
	if !isSuccessfulStatus(updateMeta.StatusCode) {
		return UpsertResult{}, formatHTTPFailure(
			fmt.Sprintf("update product %s", product.Sku),
			updateMeta.StatusCode,
			updateMeta.Body,
		)
	}

	// Si llegamos acá, el producto ya existía y quedó actualizado.
	return UpsertResult{
		Action:     "UPDATE",
		UpdateMeta: updateMeta,
	}, nil
}

// SyncStockLegacy replica el comportamiento del Excel de 2 columnas.
func (c *Client) SyncStockLegacy(ctx context.Context, providerID int, sku string, stock int) (*restyMeta, error) {
	// Primero traemos el producto actual para no perder el resto del payload.
	product, _, err := c.GetProduct(ctx, providerID, sku)
	if err != nil {
		return nil, err
	}

	// Solo tocamos stock y reenviamos el producto completo por PUT.
	product.Stock = stock
	updateMeta, err := c.UpdateProduct(ctx, providerID, sku, product)
	if err != nil {
		return nil, err
	}

	// La sincronización se considera exitosa solo con status HTTP 2xx.
	if !isSuccessfulStatus(updateMeta.StatusCode) {
		return nil, formatHTTPFailure(
			fmt.Sprintf("sync stock for sku %s", sku),
			updateMeta.StatusCode,
			updateMeta.Body,
		)
	}

	return updateMeta, nil
}

// BuildProductFromInput convierte un input ya validado al payload esperado por la API.
func (c *Client) BuildProductFromInput(providerID int, input ProductInput, categoryBranch CategoryBranch) Product {
	// Acá consolidamos el DTO interno del batch al contrato final de la API.
	return Product{
		Sku:              input.SKU,
		ProviderId:       providerID,
		Provider:         c.providerName,
		Stock:            input.Stock,
		Name:             input.Name,
		Description:      input.Description,
		ShortDescription: input.ShortDescription,
		Price:            input.Price,
		ListPrice:        input.ListPrice,
		NetPrice:         input.NetPrice,
		Taxes:            input.Taxes,
		Height:           input.Height,
		Width:            input.Width,
		Depth:            input.Depth,
		Weight:           input.WeightKilograms,
		Active:           true,
		Ean:              "",
		Brand:            input.Brand,
		CategoryBranch:   []CategoryBranch{categoryBranch},
	}
}

// ProductInput es la forma simple y estable en la que otros paquetes
// deberían entregarle datos a `productsapi`.
type ProductInput struct {
	SKU              string
	Name             string
	Brand            string
	Description      string
	ShortDescription string
	Stock            int
	Price            float64
	ListPrice        float64
	NetPrice         float64
	Taxes            float64
	Height           float64
	Width            float64
	Depth            float64
	WeightKilograms  float64
}

// isProductNotFound detecta la señal funcional que usa el legacy
// para decidir el fallback a create.
func isProductNotFound(body []byte) bool {
	var envelope ProductErrorEnvelope
	if err := json.Unmarshal(body, &envelope); err != nil {
		return false
	}

	// Esta señal textual es la que dispara el fallback de update a create.
	return strings.EqualFold(strings.TrimSpace(envelope.Result.Description), "Producto inexistente")
}

// UpsertResult devuelve suficiente contexto para logging y decisiones posteriores.
type UpsertResult struct {
	Action     string
	UpdateMeta *restyMeta
	CreateMeta *restyMeta
}

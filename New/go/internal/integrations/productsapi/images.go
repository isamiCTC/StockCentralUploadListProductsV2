package productsapi

import (
	"context"
	"encoding/json"
	"fmt"
	"net/http"
	"strings"
)

// Este archivo implementa la semántica legacy de imágenes:
// - consultar imagen por índice
// - comparar Base64 existente
// - actualizar por PUT
// - si la imagen "no existe", crear por POST

// GetProductImage obtiene la imagen existente en un índice dado.
func (c *Client) GetProductImage(ctx context.Context, providerID int, sku string, index int) (string, *restyMeta, error) {
	response, err := c.newRequest(ctx).Get(c.providerProductImagePath(providerID, sku, index))
	if err != nil {
		return "", nil, fmt.Errorf("get image %d for sku %s: %w", index, sku, err)
	}

	meta := newRestyMeta(response)
	if response.StatusCode() != http.StatusOK {
		return "", meta, fmt.Errorf("get image %d for sku %s returned status %d", index, sku, response.StatusCode())
	}

	var envelope ImageEnvelope
	if err := json.Unmarshal(response.Body(), &envelope); err != nil {
		return "", meta, fmt.Errorf("decode image envelope for sku %s index %d: %w", sku, index, err)
	}

	return envelope.Result.Base64, meta, nil
}

// UpdateProductImage reemplaza la imagen en un índice existente.
func (c *Client) UpdateProductImage(ctx context.Context, providerID int, sku string, index int, image ProductImage) (*restyMeta, error) {
	response, err := c.newRequest(ctx).SetBody(image).Put(c.providerProductImagePath(providerID, sku, index))
	if err != nil {
		return nil, fmt.Errorf("update image %d for sku %s: %w", index, sku, err)
	}

	return newRestyMeta(response), nil
}

// CreateProductImage crea una nueva imagen para el producto.
func (c *Client) CreateProductImage(ctx context.Context, providerID int, sku string, image ProductImage) (*restyMeta, error) {
	response, err := c.newRequest(ctx).SetBody(image).Post(c.providerProductImagesPath(providerID, sku))
	if err != nil {
		return nil, fmt.Errorf("create image for sku %s: %w", sku, err)
	}

	return newRestyMeta(response), nil
}

// SyncImageLegacy replica la regla del legado:
// - si existe y es igual, no subir
// - si existe y cambió, hacer PUT
// - si PUT responde "Imagen inexistente", hacer POST
func (c *Client) SyncImageLegacy(ctx context.Context, providerID int, sku string, index int, base64Image string) (ImageSyncResult, error) {
	// Paso 1: intentamos leer la imagen actual de ese índice.
	existingBase64, getMeta, getErr := c.GetProductImage(ctx, providerID, sku, index)
	// Si ya existe exactamente igual, evitamos una subida innecesaria.
	if getErr == nil && existingBase64 == base64Image {
		return ImageSyncResult{
			Action:      "SKIP_SAME_IMAGE",
			GetMeta:     getMeta,
			ImageExists: true,
		}, nil
	}

	// Paso 2: armamos el payload común para update o create.
	imagePayload := ProductImage{Base64: base64Image}
	updateMeta, updateErr := c.UpdateProductImage(ctx, providerID, sku, index, imagePayload)
	if updateErr != nil {
		return ImageSyncResult{}, updateErr
	}

	// Paso 3: si PUT respondió con la señal legacy de "no existe",
	// hacemos fallback a POST.
	if updateMeta.StatusCode == http.StatusBadRequest && isImageNotFound(updateMeta.Body) {
		createMeta, createErr := c.CreateProductImage(ctx, providerID, sku, imagePayload)
		if createErr != nil {
			return ImageSyncResult{}, createErr
		}
		if !isSuccessfulStatus(createMeta.StatusCode) {
			return ImageSyncResult{}, formatHTTPFailure(
				fmt.Sprintf("create image for sku %s", sku),
				createMeta.StatusCode,
				createMeta.Body,
			)
		}

		return ImageSyncResult{
			Action:      "CREATE",
			GetMeta:     getMeta,
			UpdateMeta:  updateMeta,
			CreateMeta:  createMeta,
			ImageExists: false,
		}, nil
	}

	// Si no hubo fallback y PUT tampoco fue exitoso, la sincronización falla.
	if !isSuccessfulStatus(updateMeta.StatusCode) {
		return ImageSyncResult{}, formatHTTPFailure(
			fmt.Sprintf("update image %d for sku %s", index, sku),
			updateMeta.StatusCode,
			updateMeta.Body,
		)
	}

	// Si llegamos acá, la imagen terminó actualizada por PUT.
	return ImageSyncResult{
		Action:      "UPDATE",
		GetMeta:     getMeta,
		UpdateMeta:  updateMeta,
		ImageExists: getErr == nil,
	}, nil
}

// isImageNotFound detecta la señal funcional usada por el legacy
// para pasar de PUT a POST en imágenes.
func isImageNotFound(body []byte) bool {
	var envelope TransactionEnvelope
	if err := json.Unmarshal(body, &envelope); err != nil {
		return false
	}

	// Esta comparación replica literalmente la marca funcional del legado.
	return strings.EqualFold(strings.TrimSpace(envelope.TransactionId), "34|Imagen inexistente")
}

// ImageSyncResult devuelve el detalle del camino tomado por la sincronización.
type ImageSyncResult struct {
	Action      string
	GetMeta     *restyMeta
	UpdateMeta  *restyMeta
	CreateMeta  *restyMeta
	ImageExists bool
}

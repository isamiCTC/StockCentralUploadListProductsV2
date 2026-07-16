package productsapi

import (
	"context"
	"fmt"
	"strings"
	"time"

	"github.com/go-resty/resty/v2"

	appconfig "stockcentraluploadlistproductsv2/internal/config"
)

// Este archivo contiene el cliente base de la API de productos.
//
// Su responsabilidad es centralizar:
// - base URL
// - token
// - timeout
// - construcción consistente de requests
//
// La semántica de cada recurso vive en archivos separados.

type Client struct {
	baseURL                 string
	token                   string
	providerName            string
	deadlockMaxAttempts     int
	deadlockBaseDelayMillis int
	http                    *resty.Client
}

// NewClient construye el cliente REST principal de la API de productos.
func NewClient(cfg appconfig.ProductsAPIConfig, token string) *Client {
	httpClient := resty.New().
		SetBaseURL(strings.TrimRight(cfg.BaseURL, "/")).
		SetTimeout(time.Duration(cfg.TimeoutSeconds) * time.Second)

	return &Client{
		baseURL:                 strings.TrimRight(cfg.BaseURL, "/"),
		token:                   token,
		providerName:            cfg.ProviderName,
		deadlockMaxAttempts:     cfg.DeadlockMaxAttempts,
		deadlockBaseDelayMillis: cfg.DeadlockBaseDelayMillis,
		http:                    httpClient,
	}
}

// newRequest arma una request con contexto, header de autenticación y JSON.
func (c *Client) newRequest(ctx context.Context) *resty.Request {
	return c.http.R().
		SetContext(ctx).
		SetHeader("Authorization", c.token).
		SetHeader("Content-Type", "application/json").
		SetHeader("Accept", "application/json")
}

// providerProductsPath devuelve la ruta base de productos para un provider.
func (c *Client) providerProductsPath(providerID int) string {
	return fmt.Sprintf("/providers/%d/products", providerID)
}

// providerProductPath devuelve la ruta de un SKU particular.
func (c *Client) providerProductPath(providerID int, sku string) string {
	return fmt.Sprintf("/providers/%d/products/%s/", providerID, sku)
}

// providerProductImagePath devuelve la ruta de una imagen puntual por índice.
func (c *Client) providerProductImagePath(providerID int, sku string, index int) string {
	return fmt.Sprintf("/providers/%d/products/%s/images/%d", providerID, sku, index)
}

// providerProductImagesPath devuelve la ruta de creación de imágenes.
func (c *Client) providerProductImagesPath(providerID int, sku string) string {
	return fmt.Sprintf("/providers/%d/products/%s/images", providerID, sku)
}

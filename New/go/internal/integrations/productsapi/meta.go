package productsapi

import (
	"fmt"
	"strings"

	"github.com/go-resty/resty/v2"
)

// Este archivo guarda metadatos mínimos de respuesta HTTP útiles para logging.
//
// No exponemos directamente `resty.Response` hacia arriba porque preferimos
// una estructura chica, estable y fácil de loguear.

type restyMeta struct {
	StatusCode int
	Body       []byte
}

// GetStatusCode expone el status HTTP de forma estable para capas externas
// que solo necesitan loguearlo o resumirlo.
func (m *restyMeta) GetStatusCode() int {
	if m == nil {
		return 0
	}
	return m.StatusCode
}

// GetBody expone una copia defensiva del body HTTP para logging.
func (m *restyMeta) GetBody() []byte {
	if m == nil {
		return nil
	}

	return append([]byte(nil), m.Body...)
}

func newRestyMeta(response *resty.Response) *restyMeta {
	return &restyMeta{
		StatusCode: response.StatusCode(),
		Body:       append([]byte(nil), response.Body()...),
	}
}

func formatHTTPFailure(action string, statusCode int, body []byte) error {
	bodyText := strings.TrimSpace(string(body))
	if bodyText == "" {
		return fmt.Errorf("%s failed with status %d", action, statusCode)
	}

	// Dejamos el body visible para diagnóstico, pero acotado para no inflar
	// demasiado los logs cuando una API devuelve HTML o payloads grandes.
	const maxBodyLength = 500
	if len(bodyText) > maxBodyLength {
		bodyText = bodyText[:maxBodyLength] + "..."
	}

	return fmt.Errorf("%s failed with status %d body=%q", action, statusCode, bodyText)
}

func isSuccessfulStatus(statusCode int) bool {
	return statusCode >= 200 && statusCode < 300
}

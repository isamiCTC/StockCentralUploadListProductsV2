package products

import "github.com/go-resty/resty/v2"

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

func newRestyMeta(response *resty.Response) *restyMeta {
	return &restyMeta{
		StatusCode: response.StatusCode(),
		Body:       append([]byte(nil), response.Body()...),
	}
}

func isSuccessfulStatus(statusCode int) bool {
	return statusCode >= 200 && statusCode < 300
}

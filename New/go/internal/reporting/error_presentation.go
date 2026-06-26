package reporting

import (
	"context"
	"encoding/json"
	"fmt"
	"strconv"
	"strings"
)

// Este archivo concentra la traducción de errores técnicos a textos finales
// aptos para el Excel que ve el cliente.
//
// La idea es mantener:
// - logs técnicos ricos para soporte;
// - y mensajes/detalles entendibles para negocio o cliente final.

// BuildErrorPresentation arma el mensaje y detalle visibles de una fila con error.
//
// `baseMessage` sigue describiendo el paso funcional que falló. El detalle se
// humaniza para evitar filtrar códigos, payloads crudos o mensajes demasiado
// internos al archivo de resultados.
func BuildErrorPresentation(ctx context.Context, baseMessage string, err error) (message, detail string) {
	if ctx.Err() == context.DeadlineExceeded {
		return "La fila excedió el timeout configurado", "El procesamiento de esta fila superó el tiempo máximo permitido."
	}
	if ctx.Err() == context.Canceled {
		return "La fila fue cancelada", "El procesamiento de esta fila fue cancelado antes de completarse."
	}

	humanDetail := humanizeTechnicalError(err)
	if humanDetail == "" {
		humanDetail = "No se pudo completar la operación solicitada."
	}

	return baseMessage, humanDetail
}

func humanizeTechnicalError(err error) string {
	if err == nil {
		return ""
	}

	raw := strings.TrimSpace(err.Error())
	if raw == "" {
		return ""
	}

	lower := strings.ToLower(raw)

	if strings.Contains(lower, "formato num") {
		return "El valor informado no tiene un formato numérico válido."
	}
	if strings.Contains(lower, "número entero sin decimales") {
		return "El valor informado debe ser un número entero sin decimales."
	}

	if strings.Contains(lower, "download image") {
		if statusCode := extractStatusCode(lower); statusCode > 0 {
			switch {
			case statusCode == 404:
				return "No se pudo descargar una imagen porque la URL informada no existe."
			case statusCode >= 500:
				return "No se pudo descargar una imagen porque el servidor de origen devolvió un error."
			default:
				return "No se pudo descargar una imagen desde la URL informada."
			}
		}

		if strings.Contains(lower, "unsupported protocol scheme") || strings.Contains(lower, "no such host") {
			return "No se pudo descargar una imagen porque la URL informada no es válida o no está disponible."
		}

		return "No se pudo descargar una imagen desde la URL informada."
	}

	if strings.Contains(lower, "failed with status") || strings.Contains(lower, "returned status") {
		return humanizeHTTPError(raw, lower)
	}

	if strings.Contains(lower, "connection refused") ||
		strings.Contains(lower, "dial tcp") ||
		strings.Contains(lower, "deadline exceeded") ||
		strings.Contains(lower, "no such host") ||
		strings.Contains(lower, "tls") ||
		strings.Contains(lower, "eof") {
		return "No se pudo establecer comunicación con el servicio externo."
	}

	if strings.Contains(lower, "decode ") && strings.Contains(lower, "envelope") {
		return "El servicio externo devolvió una respuesta que no pudo interpretarse."
	}

	if strings.Contains(lower, "invalid character") && strings.Contains(lower, "json") {
		return "El servicio externo devolvió una respuesta inválida."
	}

	return raw
}

func humanizeHTTPError(raw, lower string) string {
	statusCode := extractStatusCode(lower)
	apiMessage := extractAPIMessage(raw)

	if apiMessage != "" {
		switch {
		case statusCode >= 400 && statusCode < 500:
			return fmt.Sprintf("La API rechazó la operación: %s.", apiMessage)
		case statusCode >= 500:
			return fmt.Sprintf("La API no pudo completar la operación y devolvió este mensaje: %s.", apiMessage)
		}
	}

	switch {
	case statusCode == 400:
		return "La API rechazó la operación por los datos enviados."
	case statusCode == 401 || statusCode == 403:
		return "La API rechazó la operación por credenciales o permisos."
	case statusCode == 404:
		return "La API no encontró el recurso solicitado."
	case statusCode == 409:
		return "La API informó un conflicto al procesar la solicitud."
	case statusCode == 422:
		return "La API rechazó la operación por validaciones de negocio."
	case statusCode == 429:
		return "La API rechazó temporalmente la operación por exceso de solicitudes."
	case statusCode >= 500:
		return "La API no pudo completar la operación por un error interno."
	case statusCode > 0:
		return "La API devolvió un error y no pudo completar la operación."
	default:
		return "El servicio externo devolvió una respuesta inválida."
	}
}

func extractStatusCode(text string) int {
	index := strings.Index(text, "status ")
	if index < 0 {
		return 0
	}

	rest := text[index+len("status "):]
	end := 0
	for end < len(rest) && rest[end] >= '0' && rest[end] <= '9' {
		end++
	}
	if end == 0 {
		return 0
	}

	statusCode, err := strconv.Atoi(rest[:end])
	if err != nil {
		return 0
	}

	return statusCode
}

func extractAPIMessage(raw string) string {
	const marker = `body="`

	start := strings.Index(raw, marker)
	if start < 0 {
		return ""
	}

	quoted := raw[start+len("body="):]
	bodyText, err := strconv.Unquote(quoted)
	if err != nil {
		return ""
	}

	return extractMessageFromBody(bodyText)
}

func extractMessageFromBody(bodyText string) string {
	clean := strings.TrimSpace(bodyText)
	if clean == "" {
		return ""
	}

	var payload any
	if err := json.Unmarshal([]byte(clean), &payload); err == nil {
		if message := findFirstTextValue(payload); message != "" {
			return message
		}
	}

	if strings.ContainsAny(clean, "{}[]") {
		return ""
	}

	return strings.TrimSuffix(clean, ".")
}

func findFirstTextValue(value any) string {
	switch typed := value.(type) {
	case map[string]any:
		for _, key := range []string{"Message", "message", "Description", "description", "Error", "error", "TransactionId", "transactionId"} {
			if raw, ok := typed[key]; ok {
				if text := strings.TrimSpace(fmt.Sprint(raw)); text != "" {
					return strings.TrimSuffix(text, ".")
				}
			}
		}
		for _, nested := range typed {
			if text := findFirstTextValue(nested); text != "" {
				return text
			}
		}
	case []any:
		for _, nested := range typed {
			if text := findFirstTextValue(nested); text != "" {
				return text
			}
		}
	}

	return ""
}

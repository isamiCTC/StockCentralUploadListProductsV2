package notifications

import (
	"slices"
	"strings"

	appconfig "stockcentraluploadlistproductsv2/internal/config"
	"stockcentraluploadlistproductsv2/internal/domain"
)

// Este archivo resuelve los destinatarios finales de cada notificación.
//
// Regla acordada:
// - siempre incluir la lista fija definida en config
// - además incluir el email del provider si vino informado por el SP
// - eliminar vacíos, espacios y duplicados

// ResolveRecipients devuelve la lista final de destinatarios para un provider.
func ResolveRecipients(cfg appconfig.NotificationsConfig, provider domain.Provider) []string {
	unique := make(map[string]struct{})
	recipients := make([]string, 0, len(cfg.AlwaysRecipients)+1)

	// Primero entran los destinatarios fijos configurados para todas las corridas.
	for _, value := range cfg.AlwaysRecipients {
		addRecipient(unique, &recipients, value)
	}

	// Después sumamos el mail puntual del provider si existe.
	addRecipient(unique, &recipients, provider.Email)

	// Ordenamos para obtener un resultado estable y fácil de auditar.
	slices.Sort(recipients)
	return recipients
}

// addRecipient normaliza y agrega un mail solo si tiene contenido y no estaba.
func addRecipient(unique map[string]struct{}, recipients *[]string, raw string) {
	value := strings.TrimSpace(raw)
	if value == "" {
		return
	}

	// La comparación de duplicados se hace en minúscula, pero preservamos
	// el valor original para el envío final.
	key := strings.ToLower(value)
	if _, exists := unique[key]; exists {
		return
	}

	unique[key] = struct{}{}
	*recipients = append(*recipients, value)
}

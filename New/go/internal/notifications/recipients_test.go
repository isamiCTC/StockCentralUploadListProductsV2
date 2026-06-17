package notifications

import (
	"reflect"
	"testing"

	appconfig "stockcentraluploadlistproductsv2/internal/config"
	"stockcentraluploadlistproductsv2/internal/domain"
)

// Este archivo prueba la resolución final de destinatarios de notificación.

func TestResolveRecipientsDeduplicatesAndTrims(t *testing.T) {
	t.Parallel()

	cfg := appconfig.NotificationsConfig{
		AlwaysRecipients: []string{
			" soporte@ctcgroup.com.ar ",
			"CATALOGOS@ctcgroup.com.ar",
			"soporte@ctcgroup.com.ar",
		},
	}
	provider := domain.Provider{
		Email: " catalogos@ctcgroup.com.ar ",
	}

	got := ResolveRecipients(cfg, provider)
	want := []string{"CATALOGOS@ctcgroup.com.ar", "soporte@ctcgroup.com.ar"}
	if !reflect.DeepEqual(got, want) {
		t.Fatalf("ResolveRecipients = %#v, want %#v", got, want)
	}
}

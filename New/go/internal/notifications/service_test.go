package notifications

import (
	"context"
	"errors"
	"io"
	"reflect"
	"testing"

	appconfig "stockcentraluploadlistproductsv2/internal/config"
	"stockcentraluploadlistproductsv2/internal/intake"
	"stockcentraluploadlistproductsv2/internal/logging"
	"stockcentraluploadlistproductsv2/internal/reporting"
)

func TestNotifyFileProcessedSkipsWhenNotificationsDisabled(t *testing.T) {
	t.Parallel()

	sender := &stubMailSender{}
	service := NewService(appconfig.NotificationsConfig{
		Enabled:   false,
		FromEmail: "alerts@example.test",
	}, sender, discardNotificationLoggerSet())

	err := service.NotifyFileProcessed(context.Background(), intake.FileJob{ProviderID: 342}, reporting.FileResult{})
	if err != nil {
		t.Fatalf("NotifyFileProcessed returned error: %v", err)
	}
	if sender.calls != 0 {
		t.Fatalf("sender.calls = %d, want 0", sender.calls)
	}
}

func TestNotifyFileProcessedSendsExpectedAttachmentForProcessedWithErrors(t *testing.T) {
	t.Parallel()

	sender := &stubMailSender{}
	service := NewService(appconfig.NotificationsConfig{
		Enabled:          true,
		FromEmail:        "alerts@example.test",
		AlwaysRecipients: []string{"ops@example.test"},
	}, sender, discardNotificationLoggerSet())

	job := intake.FileJob{
		ProviderID:    342,
		ProviderName:  "Carrefour",
		ProviderEmail: "provider@example.test",
		RelativePath:  "sub/catalog.xlsx",
	}
	result := reporting.FileResult{
		Status:          reporting.FileStatusProcessedErrors,
		ResultsFilePath: "C:/processed/342/sub/catalog.result.xlsx",
	}

	err := service.NotifyFileProcessed(context.Background(), job, result)
	if err != nil {
		t.Fatalf("NotifyFileProcessed returned error: %v", err)
	}
	if sender.calls != 1 {
		t.Fatalf("sender.calls = %d, want 1", sender.calls)
	}
	if sender.fromEmail != "alerts@example.test" {
		t.Fatalf("fromEmail = %q", sender.fromEmail)
	}
	wantRecipients := []string{"ops@example.test", "provider@example.test"}
	if !reflect.DeepEqual(sender.to, wantRecipients) {
		t.Fatalf("to = %#v, want %#v", sender.to, wantRecipients)
	}
	if sender.subject != "Archivo procesado con errores - Carrefour - catalog.xlsx" {
		t.Fatalf("subject = %q", sender.subject)
	}
	if sender.attachmentPath != "C:/processed/342/sub/catalog.result.xlsx" {
		t.Fatalf("attachmentPath = %q", sender.attachmentPath)
	}
}

func TestNotifyFileProcessedUsesStructureAttachmentForStructureError(t *testing.T) {
	t.Parallel()

	sender := &stubMailSender{}
	service := NewService(appconfig.NotificationsConfig{
		Enabled:          true,
		FromEmail:        "alerts@example.test",
		AlwaysRecipients: []string{"ops@example.test"},
	}, sender, discardNotificationLoggerSet())

	job := intake.FileJob{
		ProviderID:   342,
		ProviderName: "Carrefour",
		RelativePath: "catalog.xlsx",
	}
	result := reporting.FileResult{
		Status:              reporting.FileStatusStructureError,
		StructureErrorsPath: "C:/processed/342/catalog.structure-errors.xlsx",
	}

	err := service.NotifyFileProcessed(context.Background(), job, result)
	if err != nil {
		t.Fatalf("NotifyFileProcessed returned error: %v", err)
	}
	if sender.attachmentPath != "C:/processed/342/catalog.structure-errors.xlsx" {
		t.Fatalf("attachmentPath = %q", sender.attachmentPath)
	}
	if sender.subject != "Archivo rechazado - Carrefour - catalog.xlsx" {
		t.Fatalf("subject = %q", sender.subject)
	}
}

func TestNotifyFileProcessedReturnsWrappedSenderError(t *testing.T) {
	t.Parallel()

	sender := &stubMailSender{err: errors.New("boom")}
	service := NewService(appconfig.NotificationsConfig{
		Enabled:          true,
		FromEmail:        "alerts@example.test",
		AlwaysRecipients: []string{"ops@example.test"},
	}, sender, discardNotificationLoggerSet())

	err := service.NotifyFileProcessed(context.Background(), intake.FileJob{
		ProviderID:   342,
		ProviderName: "Carrefour",
		RelativePath: "catalog.xlsx",
	}, reporting.FileResult{
		Status:          reporting.FileStatusProcessed,
		ResultsFilePath: "C:/processed/342/catalog.result.xlsx",
	})
	if err == nil {
		t.Fatal("NotifyFileProcessed should return error when sender fails")
	}
}

type stubMailSender struct {
	calls          int
	fromEmail      string
	to             []string
	subject        string
	body           string
	attachmentPath string
	err            error
}

func (s *stubMailSender) SendMail(_ context.Context, fromEmail string, to []string, subject, body, attachmentPath string) error {
	s.calls++
	s.fromEmail = fromEmail
	s.to = append([]string(nil), to...)
	s.subject = subject
	s.body = body
	s.attachmentPath = attachmentPath
	return s.err
}

func discardNotificationLoggerSet() logging.LoggerSet {
	return logging.LoggerSet{
		Summary: logging.New(logging.LevelDebug, io.Discard),
		Detail:  logging.New(logging.LevelDebug, io.Discard),
	}
}

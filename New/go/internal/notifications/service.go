package notifications

import (
	"context"
	"fmt"
	"path/filepath"

	appconfig "stockcentraluploadlistproductsv2/internal/config"
	"stockcentraluploadlistproductsv2/internal/domain"
	"stockcentraluploadlistproductsv2/internal/logging"
)

// Este archivo contiene la lógica funcional de notificaciones del batch.
//
// Responsabilidades:
// - decidir si corresponde enviar mail
// - resolver destinatarios finales
// - elegir asunto, cuerpo y adjunto
// - delegar el envío real al cliente de SendGrid

// MailSender define el contrato mínimo que necesita el servicio.
type MailSender interface {
	SendMail(ctx context.Context, fromEmail string, to []string, subject, body, attachmentPath string) error
}

// Service coordina el envío de mails al terminar cada archivo.
type Service struct {
	cfg    appconfig.NotificationsConfig
	sender MailSender
	logs   logging.LoggerSet
}

// NewService construye el servicio de notificaciones.
func NewService(cfg appconfig.NotificationsConfig, sender MailSender, logs logging.LoggerSet) *Service {
	return &Service{
		cfg:    cfg,
		sender: sender,
		logs:   logs,
	}
}

// NotifyFileProcessed envía un mail corto con el adjunto correspondiente.
//
// Si notificaciones está deshabilitado o no hay destinatarios, no falla:
// simplemente registra y sale.
func (s *Service) NotifyFileProcessed(ctx context.Context, job domain.FileJob, result domain.FileResult) error {
	if !s.cfg.Enabled {
		s.logs.Detail.Info("notification-skipped",
			logging.Int("provider_id", job.ProviderID),
			logging.String("reason", "notifications disabled"),
		)
		return nil
	}

	recipients := ResolveRecipients(s.cfg, domain.Provider{
		ID:    job.ProviderID,
		Name:  job.ProviderName,
		Email: job.ProviderEmail,
	})
	if len(recipients) == 0 {
		s.logs.Detail.Warn("notification-skipped",
			logging.Int("provider_id", job.ProviderID),
			logging.String("reason", "no recipients resolved"),
		)
		return nil
	}

	subject, body, attachmentPath := buildNotificationPayload(job, result)
	if attachmentPath == "" {
		s.logs.Detail.Warn("notification-skipped",
			logging.Int("provider_id", job.ProviderID),
			logging.String("reason", "no attachment path available"),
		)
		return nil
	}

	if err := s.sender.SendMail(ctx, s.cfg.FromEmail, recipients, subject, body, attachmentPath); err != nil {
		return fmt.Errorf("send notification for provider %d file %s: %w", job.ProviderID, job.RelativePath, err)
	}

	s.logs.Summary.Info("notification-sent",
		logging.Int("provider_id", job.ProviderID),
		logging.String("file", job.RelativePath),
		logging.Int("recipients_count", len(recipients)),
	)
	s.logs.Detail.Info("notification-sent",
		logging.Int("provider_id", job.ProviderID),
		logging.String("file", job.RelativePath),
		logging.String("attachment", attachmentPath),
	)

	return nil
}

// buildNotificationPayload define el asunto, cuerpo y adjunto según el estado.
func buildNotificationPayload(job domain.FileJob, result domain.FileResult) (subject, body, attachmentPath string) {
	filename := filepath.Base(job.RelativePath)

	switch result.Status {
	case domain.FileStatusStructureError:
		return fmt.Sprintf("Archivo rechazado - %d - %s", job.ProviderID, filename),
			fmt.Sprintf("El archivo adjunto no pudo procesarse por estructura invalida.\nArchivo: %s", filename),
			result.StructureErrorsPath
	case domain.FileStatusProcessedErrors:
		return fmt.Sprintf("Archivo procesado con errores - %d - %s", job.ProviderID, filename),
			fmt.Sprintf("Se proceso el archivo adjunto con observaciones.\nArchivo: %s", filename),
			result.ResultsFilePath
	case domain.FileStatusProcessed:
		return fmt.Sprintf("Archivo procesado - %d - %s", job.ProviderID, filename),
			fmt.Sprintf("Se proceso el archivo adjunto.\nArchivo: %s", filename),
			result.ResultsFilePath
	default:
		return fmt.Sprintf("Archivo procesado - %d - %s", job.ProviderID, filename),
			fmt.Sprintf("Se proceso el archivo adjunto.\nArchivo: %s", filename),
			result.ResultsFilePath
	}
}

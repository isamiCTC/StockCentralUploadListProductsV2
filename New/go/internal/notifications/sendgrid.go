package notifications

import (
	"context"
	"encoding/base64"
	"fmt"
	"os"
	"path/filepath"
	"strings"

	sg "github.com/sendgrid/sendgrid-go"
	sgmail "github.com/sendgrid/sendgrid-go/helpers/mail"
)

// Este archivo encapsula el acceso concreto a SendGrid.
//
// La idea es que el resto del proyecto no necesite conocer el detalle fino de
// la librería ni del armado HTTP contra SendGrid.

// SendGridClient es el adaptador concreto de envío de mails.
type SendGridClient struct {
	apiKey string
}

type mailAttachment struct {
	Content  string
	Filename string
	Type     string
}

// NewSendGridClient crea un cliente mínimo con el API key configurado.
func NewSendGridClient(apiKey string) *SendGridClient {
	return &SendGridClient{apiKey: apiKey}
}

// SendMail realiza el envío real con asunto, cuerpo corto y múltiples adjuntos.
func (c *SendGridClient) SendMail(ctx context.Context, fromEmail string, to []string, subject, body string, attachmentPaths []string) error {
	if len(to) == 0 {
		return fmt.Errorf("sendgrid recipients list is empty")
	}

	// Los adjuntos se leen y codifican antes de armar el mail.
	attachments, err := readAttachments(attachmentPaths)
	if err != nil {
		return err
	}

	// Armamos el sobre base del mensaje.
	from := sgmail.NewEmail("", fromEmail)
	message := sgmail.NewV3Mail()
	message.SetFrom(from)
	message.Subject = subject
	message.AddContent(sgmail.NewContent("text/plain", body))

	// Todos los destinatarios se agregan dentro de una única personalización.
	personalization := sgmail.NewPersonalization()
	for _, recipient := range to {
		personalization.AddTos(sgmail.NewEmail("", recipient))
	}
	message.AddPersonalizations(personalization)

	for _, attachment := range attachments {
		message.AddAttachment(&sgmail.Attachment{
			Content:     attachment.Content,
			Type:        attachment.Type,
			Filename:    attachment.Filename,
			Disposition: "attachment",
		})
	}

	// Recién acá convertimos el mensaje en una request HTTP concreta.
	request := sg.GetRequest(c.apiKey, "/v3/mail/send", "https://api.sendgrid.com")
	request.Method = "POST"
	request.Body = sgmail.GetRequestBody(message)

	// El contexto permite cancelar o cortar por timeout desde arriba.
	response, err := sg.MakeRequestWithContext(ctx, request)
	if err != nil {
		return fmt.Errorf("sendgrid request failed: %w", err)
	}

	// Cualquier status no-2xx se trata como fallo explícito de envío.
	if response.StatusCode < 200 || response.StatusCode >= 300 {
		return fmt.Errorf("sendgrid returned status %d: %s", response.StatusCode, response.Body)
	}

	return nil
}

// readAttachments levanta los archivos finales y los devuelve en base64.
func readAttachments(paths []string) ([]mailAttachment, error) {
	attachments := make([]mailAttachment, 0, len(paths))
	for _, path := range paths {
		data, readErr := os.ReadFile(path)
		if readErr != nil {
			return nil, fmt.Errorf("read notification attachment %s: %w", path, readErr)
		}

		attachments = append(attachments, mailAttachment{
			Content:  base64.StdEncoding.EncodeToString(data),
			Filename: filepath.Base(path),
			Type:     detectAttachmentContentType(path),
		})
	}

	return attachments, nil
}

func detectAttachmentContentType(path string) string {
	switch strings.ToLower(filepath.Ext(path)) {
	case ".xlsx":
		return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
	default:
		return "application/octet-stream"
	}
}

package notifications

import (
	"context"
	"encoding/base64"
	"fmt"
	"os"
	"path/filepath"

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

// NewSendGridClient crea un cliente mínimo con el API key configurado.
func NewSendGridClient(apiKey string) *SendGridClient {
	return &SendGridClient{apiKey: apiKey}
}

// SendMail realiza el envío real con asunto, cuerpo corto y un único adjunto.
func (c *SendGridClient) SendMail(ctx context.Context, fromEmail string, to []string, subject, body, attachmentPath string) error {
	if len(to) == 0 {
		return fmt.Errorf("sendgrid recipients list is empty")
	}

	attachmentContent, attachmentName, err := readAttachment(attachmentPath)
	if err != nil {
		return err
	}

	from := sgmail.NewEmail("", fromEmail)
	message := sgmail.NewV3Mail()
	message.SetFrom(from)
	message.Subject = subject
	message.AddContent(sgmail.NewContent("text/plain", body))

	personalization := sgmail.NewPersonalization()
	for _, recipient := range to {
		personalization.AddTos(sgmail.NewEmail("", recipient))
	}
	message.AddPersonalizations(personalization)

	message.AddAttachment(&sgmail.Attachment{
		Content:     attachmentContent,
		Type:        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
		Filename:    attachmentName,
		Disposition: "attachment",
	})

	request := sg.GetRequest(c.apiKey, "/v3/mail/send", "https://api.sendgrid.com")
	request.Method = "POST"
	request.Body = sgmail.GetRequestBody(message)

	response, err := sg.MakeRequestWithContext(ctx, request)
	if err != nil {
		return fmt.Errorf("sendgrid request failed: %w", err)
	}

	if response.StatusCode < 200 || response.StatusCode >= 300 {
		return fmt.Errorf("sendgrid returned status %d: %s", response.StatusCode, response.Body)
	}

	return nil
}

// readAttachment levanta el archivo final y lo devuelve en base64.
func readAttachment(path string) (contentBase64, filename string, err error) {
	data, readErr := os.ReadFile(path)
	if readErr != nil {
		return "", "", fmt.Errorf("read notification attachment %s: %w", path, readErr)
	}

	return base64.StdEncoding.EncodeToString(data), filepath.Base(path), nil
}

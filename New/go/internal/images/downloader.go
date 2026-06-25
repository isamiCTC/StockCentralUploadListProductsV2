package images

import (
	"bytes"
	"context"
	"encoding/base64"
	"fmt"
	"image"
	"image/jpeg"
	"io"
	"net/http"
	"strings"
	"time"

	_ "image/gif"
	_ "image/jpeg"
	_ "image/png"

	"golang.org/x/image/webp"
)

// Este archivo resuelve la descarga y conversión de imágenes remotas al
// formato Base64 que espera la API de productos.
//
// Comportamiento buscado:
// - si la imagen ya es un formato estándar, usar los bytes originales
// - si parece venir en WebP, convertirla a JPEG
// - devolver siempre un string Base64 listo para sincronizar

type Downloader struct {
	httpClient *http.Client
}

// NewDownloader crea el descargador con timeout.
func NewDownloader(timeout time.Duration) *Downloader {
	return &Downloader{
		httpClient: &http.Client{Timeout: timeout},
	}
}

// DownloadAsBase64 descarga una imagen por URL y la devuelve serializada.
func (d *Downloader) DownloadAsBase64(ctx context.Context, imageURL string) (string, error) {
	// El contexto permite cortar descargas largas desde la fila que la disparó.
	request, err := http.NewRequestWithContext(ctx, http.MethodGet, imageURL, nil)
	if err != nil {
		return "", fmt.Errorf("build image request for %q: %w", imageURL, err)
	}

	// Recién acá salimos a la red.
	response, err := d.httpClient.Do(request)
	if err != nil {
		return "", fmt.Errorf("download image %q: %w", imageURL, err)
	}
	defer response.Body.Close()

	if response.StatusCode < 200 || response.StatusCode >= 300 {
		return "", fmt.Errorf("download image %q returned status %d", imageURL, response.StatusCode)
	}

	data, err := io.ReadAll(response.Body)
	if err != nil {
		return "", fmt.Errorf("read image body %q: %w", imageURL, err)
	}

	// WebP debe convertirse siempre a JPEG para respetar el contrato práctico
	// de la API legacy, que intenta abrir el base64 con System.Drawing.
	if isWebPImage(data, response.Header.Get("Content-Type")) {
		return encodeWebPAsJPEGBase64(data, imageURL)
	}

	// Primer intento: si Go puede decodificarla como imagen estándar,
	// devolvemos exactamente esos bytes.
	if _, _, err := image.Decode(bytes.NewReader(data)); err == nil {
		return base64.StdEncoding.EncodeToString(data), nil
	}

	return encodeWebPAsJPEGBase64(data, imageURL)
}

func encodeWebPAsJPEGBase64(data []byte, imageURL string) (string, error) {
	// Segundo intento: si no se pudo decodificar arriba, probamos tratarla
	// como WebP y convertirla a JPEG.
	webpImage, err := webp.Decode(bytes.NewReader(data))
	if err != nil {
		return "", fmt.Errorf("decode image %q as webp: %w", imageURL, err)
	}

	// El JPEG resultante es el que finalmente se serializa a base64.
	var encoded bytes.Buffer
	if err := jpeg.Encode(&encoded, webpImage, &jpeg.Options{Quality: 90}); err != nil {
		return "", fmt.Errorf("encode webp image %q as jpeg: %w", imageURL, err)
	}

	return base64.StdEncoding.EncodeToString(encoded.Bytes()), nil
}

func isWebPImage(data []byte, contentType string) bool {
	if strings.Contains(strings.ToLower(strings.TrimSpace(contentType)), "image/webp") {
		return true
	}

	// Firma RIFF....WEBP.
	return len(data) >= 12 &&
		string(data[0:4]) == "RIFF" &&
		string(data[8:12]) == "WEBP"
}

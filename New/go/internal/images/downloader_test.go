package images

import "testing"

func TestIsWebPImageDetectsContentType(t *testing.T) {
	t.Parallel()

	if !isWebPImage([]byte("not-a-real-image"), "image/webp") {
		t.Fatal("expected image/webp content type to be detected as webp")
	}
}

func TestIsWebPImageDetectsRIFFHeader(t *testing.T) {
	t.Parallel()

	data := []byte{'R', 'I', 'F', 'F', 0x00, 0x00, 0x00, 0x00, 'W', 'E', 'B', 'P'}
	if !isWebPImage(data, "") {
		t.Fatal("expected RIFF/WEBP header to be detected as webp")
	}
}

func TestIsWebPImageIgnoresJPEGData(t *testing.T) {
	t.Parallel()

	data := []byte{0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46}
	if isWebPImage(data, "image/jpeg") {
		t.Fatal("expected jpeg payload to not be detected as webp")
	}
}

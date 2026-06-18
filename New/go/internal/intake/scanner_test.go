package intake

import (
	"context"
	"os"
	"path/filepath"
	"testing"

	"stockcentraluploadlistproductsv2/internal/providers"
)

func TestDiscoverProviderFilesFiltersByProviderAndExtensionAndKeepsRelativePath(t *testing.T) {
	t.Parallel()

	root := t.TempDir()

	mustWriteFile(t, filepath.Join(root, "342", "catalog.xlsx"))
	mustWriteFile(t, filepath.Join(root, "342", "nested", "deep.xlsx"))
	mustWriteFile(t, filepath.Join(root, "342", "ignore.txt"))
	mustWriteFile(t, filepath.Join(root, "999", "other.xlsx"))
	mustWriteFile(t, filepath.Join(root, "not-a-provider", "skip.xlsx"))

	scanner := NewScanner(root)
	providers := []providers.Provider{
		{ID: 342, Name: "Provider 342", Email: "p342@example.test"},
	}

	jobs, err := scanner.DiscoverProviderFiles(context.Background(), providers)
	if err != nil {
		t.Fatalf("DiscoverProviderFiles returned error: %v", err)
	}
	if len(jobs) != 2 {
		t.Fatalf("len(jobs) = %d, want 2", len(jobs))
	}

	got := map[string]FileJob{}
	for _, job := range jobs {
		got[filepath.ToSlash(job.RelativePath)] = job
		if job.ProviderID != 342 {
			t.Fatalf("ProviderID = %d, want 342", job.ProviderID)
		}
		if job.ProviderName != "Provider 342" {
			t.Fatalf("ProviderName = %q, want Provider 342", job.ProviderName)
		}
		if job.ProviderEmail != "p342@example.test" {
			t.Fatalf("ProviderEmail = %q, want p342@example.test", job.ProviderEmail)
		}
	}

	if _, ok := got["catalog.xlsx"]; !ok {
		t.Fatalf("catalog.xlsx was not discovered: %#v", got)
	}
	if _, ok := got["nested/deep.xlsx"]; !ok {
		t.Fatalf("nested/deep.xlsx was not discovered: %#v", got)
	}
}

func TestDiscoverProviderFilesStopsWhenContextIsCanceled(t *testing.T) {
	t.Parallel()

	root := t.TempDir()
	mustWriteFile(t, filepath.Join(root, "342", "catalog.xlsx"))

	scanner := NewScanner(root)
	ctx, cancel := context.WithCancel(context.Background())
	cancel()

	_, err := scanner.DiscoverProviderFiles(ctx, []providers.Provider{{ID: 342}})
	if err == nil {
		t.Fatal("DiscoverProviderFiles should fail when context is canceled")
	}
	if err != context.Canceled {
		t.Fatalf("err = %v, want %v", err, context.Canceled)
	}
}

func mustWriteFile(t *testing.T, path string) {
	t.Helper()

	if err := os.MkdirAll(filepath.Dir(path), 0o755); err != nil {
		t.Fatalf("MkdirAll(%s): %v", filepath.Dir(path), err)
	}
	if err := os.WriteFile(path, []byte("test"), 0o600); err != nil {
		t.Fatalf("WriteFile(%s): %v", path, err)
	}
}

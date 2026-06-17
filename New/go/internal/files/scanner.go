package files

import (
	"context"
	"fmt"
	"os"
	"path/filepath"
	"slices"
	"strconv"
	"strings"

	"stockcentraluploadlistproductsv2/internal/domain"
)

// Este archivo se ocupa de recorrer el árbol de input y descubrir los archivos
// que realmente deberían entrar al batch.
//
// Reglas actuales:
// - solo revisa subcarpetas inmediatas del input root
// - solo toma carpetas con nombre numérico
// - solo acepta carpetas cuyo número exista como provider válido
// - solo considera archivos `.xlsx`
type Scanner struct {
	inputRoot string
}

// NewScanner crea el scanner contra una raíz de input concreta.
func NewScanner(inputRoot string) *Scanner {
	return &Scanner{inputRoot: inputRoot}
}

// DiscoverProviderFiles devuelve todos los archivos Excel válidos detectados.
//
// La salida no procesa nada todavía: solo arma `FileJob`s con la identidad
// básica de cada archivo encontrado.
func (s *Scanner) DiscoverProviderFiles(ctx context.Context, providers []domain.Provider) ([]domain.FileJob, error) {
	// Leemos solo el primer nivel del input root. El resto del descenso ocurre
	// recién dentro de cada provider válido.
	entries, err := os.ReadDir(s.inputRoot)
	if err != nil {
		return nil, fmt.Errorf("read input root: %w", err)
	}

	// Construimos dos estructuras auxiliares:
	// - un mapa para resolver el provider completo por ID
	// - un slice con IDs permitidos para filtrar carpetas
	providerByID := make(map[int]domain.Provider, len(providers))
	allowed := make([]int, 0, len(providers))
	for _, provider := range providers {
		providerByID[provider.ID] = provider
		allowed = append(allowed, provider.ID)
	}
	slices.Sort(allowed)

	var jobs []domain.FileJob

	for _, entry := range entries {
		// Si el contexto fue cancelado, cortamos cuanto antes.
		if err := ctx.Err(); err != nil {
			return nil, err
		}

		// Solo nos interesan carpetas al primer nivel.
		if !entry.IsDir() {
			continue
		}

		// Si el nombre no es numérico, no puede ser un provider válido.
		providerID, err := strconv.Atoi(strings.TrimSpace(entry.Name()))
		if err != nil {
			continue
		}

		// Si el provider no vino de DB, directamente lo ignoramos.
		if !slices.Contains(allowed, providerID) {
			continue
		}

		// Desde acá ya sabemos que estamos dentro de una carpeta de provider
		// habilitado para esta corrida.
		providerRoot := filepath.Join(s.inputRoot, entry.Name())
		err = filepath.WalkDir(providerRoot, func(path string, d os.DirEntry, walkErr error) error {
			if walkErr != nil {
				return walkErr
			}
			if d.IsDir() {
				return nil
			}
			// La V2 soporta solo `.xlsx`.
			if !isExcel(path) {
				return nil
			}

			// Guardamos la ruta relativa para poder reconstruir luego la misma
			// subestructura dentro de processing y processed.
			relativePath, relErr := filepath.Rel(providerRoot, path)
			if relErr != nil {
				return fmt.Errorf("compute relative path for %s: %w", path, relErr)
			}

			// El job no procesa todavía: solo deja listo el "qué archivo"
			// y "a qué provider pertenece".
			jobs = append(jobs, domain.FileJob{
				ProviderID:    providerID,
				ProviderName:  providerByID[providerID].Name,
				ProviderEmail: providerByID[providerID].Email,
				InputPath:     path,
				RelativePath:  relativePath,
			})
			return nil
		})
		if err != nil {
			return nil, fmt.Errorf("walk provider directory %s: %w", providerRoot, err)
		}
	}

	// La lista sale plana para que el batch decida luego el orden de ejecución.
	return jobs, nil
}

// isExcel encapsula la regla actual de formato permitido.
func isExcel(path string) bool {
	ext := strings.ToLower(strings.TrimSpace(filepath.Ext(path)))
	return ext == ".xlsx"
}

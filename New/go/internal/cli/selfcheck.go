package cli

import (
	"os"

	"stockcentraluploadlistproductsv2/internal/app/selfcheck"

	"github.com/spf13/cobra"
)

// Este archivo declara el subcomando `self-check` y el error especial que usa
// la CLI para devolver códigos de salida sin mezclar mensajes de error extras.
type commandExitError struct {
	code int
}

// Error existe solo para cumplir la interfaz `error`.
// El texto vacío evita duplicar mensajes cuando el comando ya informó todo.
func (e commandExitError) Error() string {
	return ""
}

// newSelfCheckCmd crea el comando que valida ambiente sin correr el batch.
func newSelfCheckCmd(opts *options) *cobra.Command {
	return &cobra.Command{
		Use:   "self-check",
		Short: "Validate configuration, filesystem access and SQL connectivity",
		RunE: func(cmd *cobra.Command, args []string) error {
			// El self-check imprime su propio detalle por stdout.
			if exitCode := selfcheck.Execute(opts.settingsPath, opts.envPath, os.Stdout); exitCode != 0 {
				return commandExitError{code: exitCode}
			}
			return nil
		},
	}
}

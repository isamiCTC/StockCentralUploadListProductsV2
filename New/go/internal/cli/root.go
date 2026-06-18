package cli

import (
	"errors"
	"fmt"
	"os"

	"github.com/spf13/cobra"
)

// Este archivo arma el comando raíz de la CLI.
//
// Acá definimos:
// - el nombre principal del binario;
// - los flags globales compartidos por todos los subcomandos;
// - y qué subcomandos cuelgan del root.
//
// La lógica real del batch no vive acá: este paquete solo traduce argumentos
// de consola a llamadas simples a la capa `internal/app`.
type options struct {
	settingsPath string
	envPath      string
}

// Execute construye y corre la CLI completa.
func Execute() int {
	// Arrancamos con rutas por defecto para que el usuario no tenga que
	// escribirlas en cada ejecución normal.
	opts := options{
		settingsPath: "config/appsettings.toml",
		envPath:      "config/.env",
	}

	// Construimos el root y dejamos que Cobra haga el parseo de argumentos.
	rootCmd := newRootCmd(&opts)
	if err := rootCmd.Execute(); err != nil {
		// Algunos comandos devuelven un error especial que solo existe para
		// transportar un exit code. En ese caso no imprimimos ruido extra.
		var exitErr commandExitError
		if errors.As(err, &exitErr) {
			return exitErr.code
		}

		// Si fue un error "real", lo mostramos por stderr como haría cualquier CLI.
		fmt.Fprintln(os.Stderr, err)
		return 1
	}

	return 0
}

// newRootCmd crea el comando principal y le cuelga los subcomandos.
func newRootCmd(opts *options) *cobra.Command {
	cmd := &cobra.Command{
		Use:           "StockCentralUploadListProductsV2",
		Short:         "Process provider product spreadsheets as a one-shot batch",
		SilenceUsage:  true,
		SilenceErrors: true,
		RunE: func(cmd *cobra.Command, args []string) error {
			if err := cmd.Help(); err != nil {
				return err
			}
			return fmt.Errorf("no command selected; use 'run' or 'self-check'")
		},
	}

	// Estos flags quedan disponibles tanto para `run` como para `self-check`.
	cmd.PersistentFlags().StringVar(&opts.settingsPath, "settings", opts.settingsPath, "path to appsettings.toml")
	cmd.PersistentFlags().StringVar(&opts.envPath, "env", opts.envPath, "path to .env file")

	// Acá registramos cada acción concreta que el binario sabe ejecutar.
	cmd.AddCommand(newRunCmd(opts))
	cmd.AddCommand(newSelfCheckCmd(opts))

	return cmd
}

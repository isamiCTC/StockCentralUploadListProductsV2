package providers

// Este archivo define el modelo mínimo de provider que usa el batch.
//
// Su responsabilidad es expresar solo los datos que el proceso necesita para:
// - identificar al provider;
// - resolver sus carpetas;
// - y notificarlo por mail si corresponde.
//
// El legado maneja más campos, pero este paquete conserva únicamente el
// subconjunto que participa del flujo actual.
type Provider struct {
	ID    int
	Name  string
	Email string
}

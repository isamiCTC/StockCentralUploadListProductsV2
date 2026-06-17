package domain

// Provider representa el mínimo dato que hoy necesitamos de un proveedor:
// su ID, su nombre y su email operativo si el SP lo informa.
//
// En la versión legacy la entidad tiene muchos más campos, pero en esta etapa
// del batch solo modelamos lo que realmente usamos.
type Provider struct {
	ID    int
	Name  string
	Email string
}

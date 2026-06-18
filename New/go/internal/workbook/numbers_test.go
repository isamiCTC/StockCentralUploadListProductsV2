package workbook

import "testing"

// Este archivo prueba el parsing numérico flexible del importador.
//
// Buscamos blindar los casos más comunes de proveedores:
// - punto decimal
// - coma decimal
// - miles mezclados
// - enteros inválidos

func TestParseFlexibleFloat(t *testing.T) {
	t.Parallel()

	testCases := []struct {
		name  string
		input string
		want  float64
	}{
		{name: "plain dot", input: "1234.56", want: 1234.56},
		{name: "plain comma", input: "1234,56", want: 1234.56},
		{name: "thousands dot decimal comma", input: "1.234,56", want: 1234.56},
		{name: "thousands comma decimal dot", input: "1,234.56", want: 1234.56},
		{name: "spaces", input: " 1 234,50 ", want: 1234.50},
	}

	for _, tc := range testCases {
		tc := tc
		t.Run(tc.name, func(t *testing.T) {
			t.Parallel()

			got, err := ParseFlexibleFloat(tc.input)
			if err != nil {
				t.Fatalf("ParseFlexibleFloat(%q) returned error: %v", tc.input, err)
			}
			if got != tc.want {
				t.Fatalf("ParseFlexibleFloat(%q) = %v, want %v", tc.input, got, tc.want)
			}
		})
	}
}

func TestParseFlexibleFloatInvalid(t *testing.T) {
	t.Parallel()

	if _, err := ParseFlexibleFloat("abc"); err == nil {
		t.Fatal("ParseFlexibleFloat should fail for invalid numeric input")
	}
}

func TestParseFlexibleInt(t *testing.T) {
	t.Parallel()

	got, err := ParseFlexibleInt("1234")
	if err != nil {
		t.Fatalf("ParseFlexibleInt returned error: %v", err)
	}
	if got != 1234 {
		t.Fatalf("ParseFlexibleInt = %d, want 1234", got)
	}
}

func TestParseFlexibleIntRejectsDecimalValue(t *testing.T) {
	t.Parallel()

	if _, err := ParseFlexibleInt("12,5"); err == nil {
		t.Fatal("ParseFlexibleInt should fail when the numeric value is not an integer")
	}
}

package productsapi

import (
	"bytes"
	"io"
	"net/http"
	"sync"
)

// Este archivo define helpers de test para mockear respuestas HTTP sin abrir
// sockets locales, lo que hace la suite más portable en entornos restringidos.

type roundTripFunc func(*http.Request) (*http.Response, error)

func (fn roundTripFunc) RoundTrip(req *http.Request) (*http.Response, error) {
	return fn(req)
}

type callCounter struct {
	mu     sync.Mutex
	counts map[string]int
}

func newCallCounter() *callCounter {
	return &callCounter{counts: make(map[string]int)}
}

func (c *callCounter) inc(key string) {
	c.mu.Lock()
	defer c.mu.Unlock()
	c.counts[key]++
}

func (c *callCounter) get(key string) int {
	c.mu.Lock()
	defer c.mu.Unlock()
	return c.counts[key]
}

func jsonResponse(status int, body string, req *http.Request) *http.Response {
	return &http.Response{
		StatusCode: status,
		Header:     make(http.Header),
		Body:       io.NopCloser(bytes.NewBufferString(body)),
		Request:    req,
	}
}

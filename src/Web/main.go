package main

import (
	"crypto/rand"
	_ "embed"
	"encoding/json"
	"fmt"
	"log"
	"net/http"
	"strings"
)

//go:embed index.html
var indexHTML string

type guidResponse struct {
	N    string `json:"n"`
	D    string `json:"d"`
	B    string `json:"b"`
	P    string `json:"p"`
	Motd string `json:"motd"`
}

func newUUID() [16]byte {
	var uuid [16]byte
	rand.Read(uuid[:])
	uuid[6] = (uuid[6] & 0x0f) | 0x40 // version 4
	uuid[8] = (uuid[8] & 0x3f) | 0x80 // variant 10
	return uuid
}

func newGuidResponse() guidResponse {
	uuid := newUUID()
	d := fmt.Sprintf("%x-%x-%x-%x-%x", uuid[0:4], uuid[4:6], uuid[6:8], uuid[8:10], uuid[10:16])
	return guidResponse{
		N:    strings.ReplaceAll(d, "-", ""),
		D:    d,
		B:    "{" + d + "}",
		P:    "(" + d + ")",
		Motd: "Congratulations!",
	}
}

func parseAccept(header string) []string {
	var types []string
	for _, part := range strings.Split(header, ",") {
		mt := strings.TrimSpace(part)
		if i := strings.Index(mt, ";"); i != -1 {
			mt = strings.TrimSpace(mt[:i])
		}
		if mt != "" {
			types = append(types, mt)
		}
	}
	return types
}

func handler(w http.ResponseWriter, r *http.Request) {
	mediaTypes := parseAccept(r.Header.Get("Accept"))
	if ct := r.Header.Get("Content-Type"); ct != "" {
		mediaTypes = append(mediaTypes, parseAccept(ct)...)
	}

	for _, mt := range mediaTypes {
		switch mt {
		case "application/json":
			guid := newGuidResponse()
			w.Header().Set("Content-Type", "application/json")
			buf, _ := json.MarshalIndent(guid, "", "  ")
			w.Write(buf)
			return
		case "text/plain":
			guid := newGuidResponse()
			w.Header().Set("Content-Type", "text/plain")
			fmt.Fprint(w, guid.D)
			return
		case "text/html":
			guid := newGuidResponse()
			page := strings.Replace(indexHTML, "{{B}}", guid.B, 1)
			page = strings.Replace(page, "{{P}}", guid.P, 1)
			page = strings.Replace(page, "{{D}}", guid.D, 1)
			page = strings.Replace(page, "{{N}}", guid.N, 1)
			w.Header().Set("Content-Type", "text/html; charset=utf-8")
			fmt.Fprint(w, page)
			return
		}
	}

	// default to text/plain
	guid := newGuidResponse()
	w.Header().Set("Content-Type", "text/plain")
	fmt.Fprint(w, guid.D)
}

func main() {
	http.HandleFunc("/", handler)
	fmt.Println("Listening on :80")
	log.Fatal(http.ListenAndServe(":80", nil))
}

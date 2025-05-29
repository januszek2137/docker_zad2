# CI/CD – wielo-architektoniczny obraz .NET 8 (Zadanie 2)

[![Build & Scan](https://github.com/januszek2137/docker_zad2/actions/workflows/container.yml/badge.svg)](https://github.com/januszek2137/docker_zad2/actions/workflows/container.yml)

---

## 1 · Treść zadania & cele

> **Wymagania:**  
> 1. Zbudować obraz z Dockerfile + kodu aplikacji (.NET 8) i wypchnąć go do **GHCR**.  
> 2. Obraz **musi wspierać** `linux/amd64` **i** `linux/arm64`.  
> 3. Budowa **ma używać** cache BuildKit-a (`registry`, `mode=max`) w publicznym repo Docker Hub.  
> 4. Przed publikacją uruchomić **skan CVE**; przy zagrożeniach *HIGH/CRITICAL* push musi zostać zablokowany.  
> 5. W sprawozdaniu należy opisać przyjętą strategię tagowania obrazu i cache.

---

## 2 · Sekrety (GitHub → Settings → *Secrets and variables* → *Actions*)

| Nazwa        | Wartość / opis                      | Kiedy używany                    |
|--------------|-------------------------------------|----------------------------------|
| `DOCKERHUB_USERNAME` | login Docker Hub               | logowanie + cache-push/pull      |
| `DOCKERHUB_TOKEN`    | Access Token `dckr_pat_…` (*RW*)| logowanie + cache-push/pull      |
| `GITHUB_TOKEN`       | wbudowany, `packages:write`   | push do GHCR                     |

---

## 3 · Pipeline (`.github/workflows/container.yml`)

### 3.1 Wyzwalacze

- **`push`** → gałąź **`master`** / tag semver **`v*.*.*`**  
- **`pull_request`** → **`master`**  
- **`workflow_dispatch`** (przycisk **Run workflow**)

### 3.2 Kluczowe kroki

| # | Krok | Najważniejsze ustawienia | Spełniony punkt zadania |
|---|------|--------------------------|-------------------------|
| 1 | **Build & load (scan)** | `platforms: linux/amd64`, `load: true`<br>`cache-from/-to: registry,mode=max` | cache (3) |
| 2 | **Trivy scan** | `scan-type: image`, `vuln-type: library`,<br>`severity: CRITICAL,HIGH`, `exit-code: 1` | blokada CVE (4) |
| 3 | **Multi-arch build & push** | `platforms: linux/amd64,linux/arm64`, `push: true`, GHCR login | wielo-arch + publikacja (2 & 1) |

<details>
<summary>Kod YAML workflow</summary>

```yaml
name: Build-scan-publish container

on:
  push:
    branches: [ master ]
    tags: [ 'v*.*.*' ]
  pull_request:
    branches: [ master ]
  workflow_dispatch:

permissions:
  contents: read
  packages: write

env:
  IMAGE_NAME: ${{ github.repository }}
  CACHE_REPO: ${{ secrets.DOCKERHUB_USERNAME }}/docker_zad1-cache
  SCAN_IMAGE: docker_zad1_scan:latest

jobs:
  build-and-scan:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: docker/setup-qemu-action@v3
      - uses: docker/setup-buildx-action@v3

      - name: Log in to Docker Hub (cache)
        uses: docker/login-action@v3
        with:
          registry: docker.io
          username: ${{ secrets.DOCKERHUB_USERNAME }}
          password: ${{ secrets.DOCKERHUB_TOKEN }}

      - name: Build & load image for scan
        uses: docker/build-push-action@v6
        with:
          context: .
          file: ./docker_zad1/Dockerfile
          platforms: linux/amd64
          load: true
          push: false
          cache-from: type=registry,ref=docker.io/${{ env.CACHE_REPO }}
          cache-to:   type=registry,ref=docker.io/${{ env.CACHE_REPO }},mode=max
          tags: ${{ env.SCAN_IMAGE }}

      - name: Scan built image (Trivy)
        uses: aquasecurity/trivy-action@0.18.0
        with:
          vuln-type: library
          scan-type: image
          image-ref: ${{ env.SCAN_IMAGE }}
          severity: CRITICAL,HIGH
          exit-code: 1
          format: table

  push-image:
    needs: build-and-scan
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: docker/setup-qemu-action@v3
      - uses: docker/setup-buildx-action@v3

      - name: Log in to GHCR
        uses: docker/login-action@v3
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}

      - name: Log in to Docker Hub (cache)
        uses: docker/login-action@v3
        with:
          registry: docker.io
          username: ${{ secrets.DOCKERHUB_USERNAME }}
          password: ${{ secrets.DOCKERHUB_TOKEN }}

      - id: meta
        uses: docker/metadata-action@v5
        with:
          images: ghcr.io/${{ env.IMAGE_NAME }}
          tags: |
            type=ref,event=branch,enable=true
            type=sha,format=short
            type=semver,pattern={{version}}
          labels: |
            org.opencontainers.image.source=${{ github.repositoryUrl }}

      - name: Build & push multi-arch
        uses: docker/build-push-action@v6
        with:
          context: .
          file: ./docker_zad1/Dockerfile
          platforms: linux/amd64,linux/arm64
          push: true
          tags: ${{ steps.meta.outputs.tags }}
          labels: ${{ steps.meta.outputs.labels }}
          cache-from: type=registry,ref=docker.io/${{ env.CACHE_REPO }}
          cache-to:   type=registry,ref=docker.io/${{ env.CACHE_REPO }},mode=max
```
</details>

## 4 · Strategia tagowania

| Tag        | Kiedy nadawany                | Cel                                       |
|------------|------------------------------|-------------------------------------------|
| `vX.Y.Z`   | push tagu semver             | jednoznaczne wersjonowanie release’u      |
| `master`   | push do głównej gałęzi       | „latest stable”                           |
| `sha-<7>`  | każdy commit                 | pełna odtwarzalność konkretnego builda    |

**Cache**: jeden obraz   
`docker.io/<user>/docker_zad1-cache:buildcache` — `mode=max` pozwala BuildKit-owi
nadpisywać warstwy i skracać czas kolejnych kompilacji  
(dokumentacja BuildKit → *registry cache*).

---

## 5 · Dlaczego wybrano **Trivy** zamiast Docker Scout?

| Kryterium                           | Trivy | Docker Scout | Uzasadnienie wyboru |
|-------------------------------------|-------|--------------|---------------------|
| Dostępna gotowa akcja w Marketplace | tak   | tak          | obie mają akcje, ale Trivy działa bez dodatkowych tokenów Dockera |
| Potrafi skanować obraz w lokalnym demonie (`load:true`) | tak | nie | pozwala zrezygnować z eksportu TAR/OCI i skrócić pipeline |
| Konfigurowalny parametr `exit-code` (blokada builda) | tak | tylko w planach | umożliwia proste „gate” przy HIGH/CRITICAL |
| Licencja i limity                   | open-source, brak limitów | SaaS, wymaga konta Docker Hub | brak dodatkowych kosztów i integracji |
| Szerokość bazy danych CVE           | niezależne repo Trivy DB | zależne od Docker Hub | Trivy posiada własną, często aktualizowaną bazę |

> **Wniosek:** Trivy spełnia wszystkie wymagania zadania przy mniejszej
> złożoności konfiguracji, dlatego został użyty jako skaner CVE.

---

## 6 · Rezultaty

| Element                                        | Status |
|------------------------------------------------|:------:|
| Build `amd64` + Trivy scan                     | ✅ |
| Multi-arch (`amd64`,`arm64`) build & push       | ✅ |
| Cache w Docker Hub (pull / push)               | ✅ |
| Skan CVE — brak **CRITICAL/HIGH**              | ✅ |
| Publikacja obrazu do GHCR                      | ✅ |

Zielony badge na górze READ-ME potwierdza, że pipeline został uruchomiony
co najmniej raz z sukcesem.

---

Autor sprawozdania: Jan Ożga

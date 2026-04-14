FROM golang:1.23-alpine AS build
WORKDIR /app
COPY src/Web/ .
RUN CGO_ENABLED=0 go build -ldflags="-s -w" -o server .

FROM scratch
COPY --from=build /app/server /server
EXPOSE 80
ENTRYPOINT ["/server"]

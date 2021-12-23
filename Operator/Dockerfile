FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS builder

COPY ./ /source
WORKDIR /source
RUN dotnet publish -r linux-musl-x64 --self-contained --runtime -c release -o /app

FROM mcr.microsoft.com/dotnet/runtime:6.0-alpine
COPY --from=builder /app /app
WORKDIR /app

RUN chmod +x /app/mariadb-operator \
 && addgroup -g 1000 -S jitesoft \
 && adduser -u 1000 -H -D -G jitesoft jitesoft

USER jitesoft

ENTRYPOINT [ "/app/mariadb-operator" ]

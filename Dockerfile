# syntax = docker/dockerfile:experimental
FROM mcr.microsoft.com/dotnet/runtime:6.0-alpine
RUN --mount=type=bind,source=./dist,target=/tmp/dist \
    mkdir /mariadb-operator \
 && cp /tmp/dist/* /mariadb-operator/ \
 && addgroup -g 1000 -S jitesoft \
 && adduser -u 1000 -H -D -G jitesoft jitesoft \
 && chmod +x /mariadb-operator/mariadb-operator

WORKDIR /mariadb-operator

USER jitesoft
ENTRYPOINT [ "/mariadb-operator/mariadb-operator" ]

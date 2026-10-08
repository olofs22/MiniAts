FROM postgres:17

ENV POSTGRES_USER=miniats
ENV POSTGRES_PASSWORD=miniats
ENV POSTGRES_DB=miniats

COPY *.sql /docker-entrypoint-initdb.d/

EXPOSE 5432
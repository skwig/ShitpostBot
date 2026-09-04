alias c := clean

alias fmt := format
alias f := format

alias l := lint

alias e2e := test-e2e

# dotnet local tools cannot be ran outside of their manifest dir

dev:
    docker compose -f docker-compose.yml -f docker-compose.Development.Linux.yml up --build

clean:
    dotnet clean -c Debug ./src/ShitpostBot/
    dotnet clean -c Release ./src/ShitpostBot/

format:
    cd ./src/ShitpostBot/ && dotnet csharpier format .

    black ./src/ShitpostBot.MlService/

lint:
    cd ./src/ShitpostBot/ && dotnet csharpier check .
    cd ./src/ShitpostBot/ && dotnet format style
    cd ./src/ShitpostBot/ && dotnet format analyzers

    mypy ./src/ShitpostBot.MlService/

    helm lint ./charts/shitpostbot/
    helm template ./charts/shitpostbot >/dev/null

test-e2e:
    docker compose -f docker-compose.yml -f docker-compose.Development.Linux.yml down
    docker compose -f docker-compose.yml -f docker-compose.Development.Linux.yml up --build --wait webapi
    ijhttp --no-progress ./test/e2e/e2e-tests.http

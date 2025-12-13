# ShitpostBot

## Helm Chart

Install ShitpostBot using Helm:

```bash
# Add the Helm repository
helm repo add shitpostbot https://skwig.github.io/ShitpostBot/
helm repo update

# Install the chart
helm install my-shitpostbot shitpostbot/shitpostbot --namespace shitpostbot --create-namespace
```

See the [Helm repository](https://skwig.github.io/ShitpostBot/) for more details.

## Run dev env
```shell
docker compose -f docker-compose.yml -f docker-compose.Development.Linux.yml up --build
```
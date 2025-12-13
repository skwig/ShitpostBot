# ShitpostBot Helm Repository

Add this Helm repository:

```bash
helm repo add shitpostbot https://skwig.github.io/ShitpostBot/
helm repo update
```

Install the chart:

```bash
helm install my-shitpostbot shitpostbot/shitpostbot
```

The chart automatically uses the correct Docker image tags via appVersion.

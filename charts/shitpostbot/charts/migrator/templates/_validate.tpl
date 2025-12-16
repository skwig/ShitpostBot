{{- if and (not .Values.config.create) (not .Values.config.name) }}
{{- fail "config.create=false requires config.name to be set" }}
{{- end }}
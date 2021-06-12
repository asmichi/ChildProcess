# Build:
#   docker build -t asmichi/node-azuredevops:14-alpine .
#
# See https://docs.microsoft.com/en-us/azure/devops/pipelines/process/container-phases?view=azure-devops#non-glibc-based-containers
FROM node:14-alpine

RUN apk add --no-cache --virtual .pipeline-deps readline linux-pam \
  && apk add --no-cache \
    # Azure Pipeline depndencies
    bash \
    sudo \
    shadow \
    # .NET Core dependencies
    icu-libs \
    ca-certificates \
    krb5-libs \
    libgcc \
    libintl \
    libssl1.1 \
    libstdc++ \
    zlib \
  && apk del .pipeline-deps

LABEL "com.azure.dev.pipelines.agent.handler.node.path"="/usr/local/bin/node"

CMD [ "node" ]

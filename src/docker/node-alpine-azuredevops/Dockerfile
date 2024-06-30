# Build:
#   docker build -t asmichi/node-azuredevops:20-alpine3.20 .
#
# https://docs.microsoft.com/en-us/azure/devops/pipelines/process/container-phases?view=azure-devops#non-glibc-based-containers
# https://learn.microsoft.com/en-us/dotnet/core/install/linux-alpine
FROM node:20-alpine3.20

RUN apk add --no-cache --virtual .pipeline-deps readline linux-pam \
  && apk add --no-cache \
    # Azure Pipeline depndencies
    bash \
    sudo \
    shadow \
    # .NET Core dependencies
    ca-certificates-bundle \
    libgcc \
    libssl3 \
    libstdc++ \
    zlib \
    && apk del .pipeline-deps

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT="1"

LABEL "com.azure.dev.pipelines.agent.handler.node.path"="/usr/local/bin/node"

CMD [ "node" ]

FROM alpine:3.20.3

WORKDIR /frp

RUN wget -qO- https://github.com/fatedier/frp/releases/download/v0.60.0/frp_0.60.0_linux_amd64.tar.gz | tar xvz

ENTRYPOINT ["frp_0.60.0_linux_amd64/frps"]

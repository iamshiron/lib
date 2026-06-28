using Shiron.Lib.DockerUtils;
using Shiron.Lib.DockerUtils.Model;
using Xunit;

namespace Shiron.Lib.Tests.DockerUtils;

public class ComposeReaderTests {
    private static Service ReadSingle(string yaml) {
        var services = new ComposeReader().Read(yaml);
        Assert.Single(services);
        return services[0];
    }

    [Fact]
    public void Read_FullService_MapsAllFields() {
        var service = ReadSingle("""
                                 services:
                                   web:
                                     image: nginx:latest
                                     container_name: web-app
                                     restart: unless-stopped
                                     ports:
                                       - "8080:80"
                                     volumes:
                                       - ./html:/usr/share/nginx/html
                                     environment:
                                       NGINX_HOST: example.com
                                     networks:
                                       - frontend
                                 """);

        Assert.Equal("web", service.Name);
        Assert.Equal("nginx:latest", service.Image);
        Assert.Equal("web-app", service.ContainerName);
        Assert.Equal(RestartAction.UnlessStopped, service.Restart);
        Assert.Single(service.Ports);
        Assert.Equal(["./html:/usr/share/nginx/html"], service.Volumes);
        Assert.Equal("example.com", service.Environment["NGINX_HOST"]);
        Assert.Equal(["frontend"], service.Networks);
    }

    [Fact]
    public void Read_Ports_HostAndContainer() {
        var service = ReadSingle("""
                                 services:
                                   web:
                                     image: nginx
                                     ports:
                                       - "8080:80"
                                 """);
        var port = Assert.Single(service.Ports);
        Assert.Equal("80", port.ContainerPort);
        Assert.Equal("8080", port.HostPort);
        Assert.Null(port.HostAddress);
        Assert.Null(port.Protocol);
    }

    [Fact]
    public void Read_Ports_WithIpAddress() {
        var service = ReadSingle("""
                                 services:
                                   web:
                                     image: nginx
                                     ports:
                                       - "127.0.0.1:8443:443"
                                 """);
        var port = Assert.Single(service.Ports);
        Assert.Equal("443", port.ContainerPort);
        Assert.Equal("8443", port.HostPort);
        Assert.Equal("127.0.0.1", port.HostAddress);
    }

    [Fact]
    public void Read_Ports_WithProtocol() {
        var service = ReadSingle("""
                                 services:
                                   web:
                                     image: nginx
                                     ports:
                                       - "127.0.0.1:9090:9090/udp"
                                 """);
        var port = Assert.Single(service.Ports);
        Assert.Equal("9090", port.ContainerPort);
        Assert.Equal("9090", port.HostPort);
        Assert.Equal("127.0.0.1", port.HostAddress);
        Assert.Equal("udp", port.Protocol);
    }

    [Fact]
    public void Read_Ports_SingleContainerPort() {
        var service = ReadSingle("""
                                 services:
                                   web:
                                     image: nginx
                                     ports:
                                       - "3000"
                                 """);
        var port = Assert.Single(service.Ports);
        Assert.Equal("3000", port.ContainerPort);
        Assert.Equal("3000", port.HostPort);
        Assert.Null(port.HostAddress);
    }

    [Fact]
    public void Read_Ports_LongForm() {
        var service = ReadSingle("""
                                 services:
                                   web:
                                     image: nginx
                                     ports:
                                       - target: 80
                                         published: 8080
                                         protocol: tcp
                                         host_ip: 127.0.0.1
                                 """);
        var port = Assert.Single(service.Ports);
        Assert.Equal("80", port.ContainerPort);
        Assert.Equal("8080", port.HostPort);
        Assert.Equal("tcp", port.Protocol);
        Assert.Equal("127.0.0.1", port.HostAddress);
    }

    [Fact]
    public void Read_Environment_AsList_FlattensToDictionary() {
        var service = ReadSingle("""
                                 services:
                                   web:
                                     image: nginx
                                     environment:
                                       - FOO=bar
                                       - EMPTY=
                                       - PASSTHROUGH
                                 """);
        Assert.Equal(3, service.Environment.Count);
        Assert.Equal("bar", service.Environment["FOO"]);
        Assert.Equal("", service.Environment["EMPTY"]);
        Assert.Null(service.Environment["PASSTHROUGH"]);
    }

    [Fact]
    public void Read_Environment_AsMap_FlattensToDictionary() {
        var service = ReadSingle("""
                                 services:
                                   web:
                                     image: nginx
                                     environment:
                                       FOO: bar
                                       EMPTY: ""
                                 """);
        Assert.Equal("bar", service.Environment["FOO"]);
        Assert.Equal("", service.Environment["EMPTY"]);
    }

    [Fact]
    public void Read_Environment_AsMap_EmptyValue() {
        var service = ReadSingle("""
                                 services:
                                   web:
                                     image: nginx
                                     environment:
                                       PASSTHROUGH:
                                       EXPLICIT: ""
                                 """);
        Assert.True(service.Environment.ContainsKey("PASSTHROUGH"));
        Assert.Equal("", service.Environment["PASSTHROUGH"]);
        Assert.Equal("", service.Environment["EXPLICIT"]);
    }

    [Fact]
    public void Read_Environment_ListValueKeepsEquals() {
        var service = ReadSingle("""
                                 services:
                                   web:
                                     image: nginx
                                     environment:
                                       - CONN=postgres://u:p@host/db
                                 """);
        Assert.Equal("postgres://u:p@host/db", service.Environment["CONN"]);
    }

    [Fact]
    public void Read_Networks_AsList() {
        var service = ReadSingle("""
                                 services:
                                   web:
                                     image: nginx
                                     networks:
                                       - frontend
                                       - backend
                                 """);
        Assert.Equal(["frontend", "backend"], service.Networks);
    }

    [Fact]
    public void Read_Networks_AsMap_ExtractsKeys() {
        var service = ReadSingle("""
                                 services:
                                   web:
                                     image: nginx
                                     networks:
                                       frontend:
                                         aliases:
                                           - web-1
                                       backend: {}
                                 """);
        Assert.Equal(["frontend", "backend"], service.Networks);
    }

    [Theory]
    [InlineData("always", RestartAction.Always)]
    [InlineData("on-failure", RestartAction.OnFailure)]
    [InlineData("unless-stopped", RestartAction.UnlessStopped)]
    public void Read_Restart_KnownPolicies(string policy, RestartAction expected) {
        var service = ReadSingle($$"""
                                   services:
                                     web:
                                       image: nginx
                                       restart: {{policy}}
                                   """);
        Assert.Equal(expected, service.Restart);
    }

    [Fact]
    public void Read_Restart_NoPolicy_IsNull() {
        var service = ReadSingle("""
                                 services:
                                   web:
                                     image: nginx
                                     restart: "no"
                                 """);
        Assert.Null(service.Restart);
    }

    [Fact]
    public void Read_Restart_OnFailureWithCount() {
        var service = ReadSingle("""
                                 services:
                                   web:
                                     image: nginx
                                     restart: "on-failure:5"
                                 """);
        Assert.Equal(RestartAction.OnFailure, service.Restart);
    }

    [Fact]
    public void Read_MultipleServices() {
        var services = new ComposeReader().Read("""
                                               services:
                                                 web:
                                                   image: nginx
                                                 db:
                                                   image: postgres
                                               """);
        Assert.Equal(2, services.Count);
        Assert.Equal("web", services[0].Name);
        Assert.Equal("db", services[1].Name);
    }

    [Fact]
    public void Read_Empty_ReturnsEmpty() {
        Assert.Empty(new ComposeReader().Read(""));
        Assert.Empty(new ComposeReader().Read("   "));
    }

    [Fact]
    public void Read_NoServicesKey_ReturnsEmpty() {
        Assert.Empty(new ComposeReader().Read("version: '3'"));
    }

    [Fact]
    public void Read_MissingImage_Throws() {
        var ex = Assert.Throws<ComposeReadException>(() => new ComposeReader().Read("""
                                                                                    services:
                                                                                      web:
                                                                                        build: .
                                                                                    """));
        Assert.Contains("missing required 'image'", ex.Message);
    }

    [Fact]
    public void Read_MalformedPort_Throws() {
        var ex = Assert.Throws<ComposeReadException>(() => new ComposeReader().Read("""
                                                                                    services:
                                                                                      web:
                                                                                        image: nginx
                                                                                        ports:
                                                                                          - "a:b:c:d"
                                                                                    """));
        Assert.Contains("unexpected format", ex.Message);
    }

    [Fact]
    public void Read_UnknownRestartPolicy_Throws() {
        Assert.Throws<ComposeReadException>(() => new ComposeReader().Read("""
                                                                           services:
                                                                             web:
                                                                               image: nginx
                                                                               restart: on-success
                                                                           """));
    }

    [Fact]
    public void Read_LongFormVolume_Throws() {
        Assert.Throws<ComposeReadException>(() => new ComposeReader().Read("""
                                                                           services:
                                                                             web:
                                                                               image: nginx
                                                                               volumes:
                                                                                 - type: bind
                                                                                   source: ./data
                                                                                   target: /data
                                                                           """));
    }
}

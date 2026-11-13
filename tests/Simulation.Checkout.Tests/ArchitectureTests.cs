using NetArchTest.Rules;
using Simulation.Checkout.Application;
using Simulation.Checkout.Application.Abstractions;
using Simulation.Checkout.Domain;
using Simulation.Checkout.Infrastructure;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Xunit;

public class ArchitectureTests
{
    // غيّر الأسماء دي لأسماء الـ namespaces عندك
    private const string DomainNs = "Simulation.Checkout.Domain";
    private const string ApplicationNs = "Simulation.Checkout.Application";
    private const string InfrastructureNs = "Simulation.Checkout.Infrastructure";

    // أي class موجود في المشروع الرئيسي (مش مشروع الاختبارات)
    private static readonly Assembly DomainAssembly = typeof(Order).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(CheckoutService).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(InMemoryOrderRepository).Assembly;

    // 5.1
    [Fact]
    public void Domain_ShouldNotDependOn_ApplicationOrInfrastructure()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That().ResideInNamespace(DomainNs)
            .ShouldNot().HaveDependencyOnAny(ApplicationNs, InfrastructureNs)
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    // 5.2
    [Fact]
    public void Application_ShouldNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That().ResideInNamespace(ApplicationNs)
            .ShouldNot().HaveDependencyOn(InfrastructureNs)
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    // 5.3
    [Fact]
    public void Interfaces_ShouldStartWith_I()
    {
        var result = Types.InAssemblies(new[] { DomainAssembly, ApplicationAssembly, InfrastructureAssembly })
    .That().AreInterfaces()
    .Should().HaveNameStartingWith("I")
    .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    // 5.4
    [Fact]
    public void IOrderRepository_Implementations_ShouldEndWith_Repository()
    {
        var result = Types.InAssemblies(new[] { DomainAssembly, ApplicationAssembly, InfrastructureAssembly })
            .That().ImplementInterface(typeof(IOrderRepository))
            .And().AreClasses()
            .Should().HaveNameEndingWith("Repository")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    // بتطبع أسماء الـ types اللي كسرت القاعدة عشان تعرف مين المشكلة
    private static string Describe(TestResult result) =>
        result.IsSuccessful
            ? string.Empty
            : "Failing types: " + string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>());
}
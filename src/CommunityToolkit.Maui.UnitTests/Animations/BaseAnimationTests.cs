﻿using System.Diagnostics;
using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Animations;

public abstract class BaseAnimationTests<TAnimation> : BaseTest where TAnimation : BaseAnimation, new()
{
	protected virtual TAnimation CreateAnimation() => new();

	[Fact]
	public async Task LengthShouldDictateFullAnimationLength()
	{
		var animation = CreateAnimation();

		Label label = new();

		label.EnableAnimations();

		var stopwatch = new Stopwatch();
		stopwatch.Start();
		await animation.Animate(label);
		stopwatch.Stop();

		stopwatch.ElapsedMilliseconds.Should().BeCloseTo(animation.Length, 50);

		stopwatch.Reset();
	}
}

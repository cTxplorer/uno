﻿using Uno.Extensions;
using Windows.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Uno.Disposables;

#if XAMARIN_IOS_UNIFIED
using Foundation;
using UIKit;
using CoreGraphics;
using LayoutInfo = System.Collections.Generic.Dictionary<Foundation.NSIndexPath, UIKit.UICollectionViewLayoutAttributes>;
#elif XAMARIN_IOS
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.CoreGraphics;
using CGRect = System.Drawing.RectangleF;
using nfloat = System.Single;
using nint = System.Int32;
using CGPoint = System.Drawing.PointF;
using CGSize = System.Drawing.SizeF;
#endif

namespace Windows.UI.Xaml.Controls
{
	/// <summary>
	/// A native layout which implements <see cref="ItemsStackPanel"/> behaviour.
	/// </summary>
	internal partial class ItemsStackPanelLayout : VirtualizingPanelLayout
	{
		#region Properties
		internal override bool SupportsDynamicItemSizes => true;
		#endregion

		public ItemsStackPanelLayout() { }

		protected override nfloat LayoutItemsInGroup(int group, nfloat availableBreadth, nfloat availableExtent, ref CGRect frame, bool createLayoutInfo, Dictionary<NSIndexPath, CGSize?> oldItemSizes)
		{

			var numberOfItems = CollectionView.NumberOfItemsInSection(group);

			_sectionEnd[group] = GetExtentEnd(frame);

			nfloat measuredBreadth = 0;
			for (var row = 0; row < numberOfItems; ++row)
			{
				var indexPath = GetNSIndexPathFromRowSection(row, group);
				SetFrameSizeForIndexPath(ref frame, oldItemSizes, indexPath, availableBreadth, availableExtent);

				if (ShouldBreadthStretch)
				{
					//We are stretched, give the maximum breadth available
					SetBreadth(ref frame, availableBreadth);
				}

				if (createLayoutInfo)
				{
					CreateItemLayoutInfo(row, group, frame);
				}

				_sectionEnd[group] = GetExtentEnd(frame);

				IncrementExtent(ref frame);
				measuredBreadth = NMath.Max(measuredBreadth, GetBreadth(frame.Size));
			}
			return measuredBreadth;
		}

		private void SetFrameSizeForIndexPath(ref CGRect frame, Dictionary<NSIndexPath, CGSize?> oldItemSizes, NSIndexPath indexPath, nfloat availableBreadth, nfloat availableExtent)
		{
			var isInView = GetExtentStart(frame) >= GetExtent(CollectionView.ContentOffset) && GetExtentStart(frame) <= (GetExtent(CollectionView.ContentOffset) + availableExtent);
			frame.Size = oldItemSizes?.UnoGetValueOrDefault(indexPath) ?? GetItemSizeForIndexPath(indexPath, isInView, availableBreadth);

		}

		private void SetExtentStart(ref CGRect frame, nfloat extentStart)
		{
			if (ScrollOrientation == Orientation.Vertical)
			{
				frame.Y = extentStart;
			}
			else
			{
				frame.X = extentStart;
			}
		}

		private protected override void UpdateLayoutAttributesForItem(UICollectionViewLayoutAttributes updatingItem, bool shouldRecurse)
		{
			//Update extent of either subsequent item in group, subsequent group header, or footer
			var currentIndex = updatingItem.IndexPath;
			var nextIndexInGroup = GetNSIndexPathFromRowSection(currentIndex.Row + 1, currentIndex.Section);

			// Get next item in current group
			var elementToAdjust = LayoutAttributesForItem(nextIndexInGroup);

			if (elementToAdjust == null)
			{
				// No more items in current group, get group header of next group
				elementToAdjust = LayoutAttributesForSupplementaryView(
					NativeListViewBase.ListViewSectionHeaderElementKindNS,
					GetNSIndexPathFromRowSection(0, currentIndex.Section + 1));

				//This is the last item in section, update information used by sticky headers.
				_sectionEnd[currentIndex.Section] = GetExtentEnd(updatingItem.Frame);
			}

			if (elementToAdjust == null)
			{
				// No more groups in source, get footer
				elementToAdjust = LayoutAttributesForSupplementaryView(
					NativeListViewBase.ListViewFooterElementKindNS,
					GetNSIndexPathFromRowSection(0, 0));
			}

			if (elementToAdjust == null)
			{
				return;
			}

			if (elementToAdjust.RepresentedElementKind != NativeListViewBase.ListViewSectionHeaderElementKind)
			{
				//Update position of subsequent item based on position of this item, which may have changed
				var frame = elementToAdjust.Frame;
				SetExtentStart(ref frame, GetExtentEnd(updatingItem.Frame));
				elementToAdjust.Frame = frame;

				if (shouldRecurse)
				{
					UpdateLayoutAttributesForItem(elementToAdjust, shouldRecurse: true);
				}
			}
			else
			{
				//Update group header
				var inlineFrame = GetInlineHeaderFrame(elementToAdjust.IndexPath.Section);
				var extentDifference = GetExtentEnd(updatingItem.Frame) - GetExtentStart(inlineFrame);
				if (extentDifference != 0)
				{
					UpdateLayoutAttributesForGroupHeader(elementToAdjust, extentDifference, true);
				}
			}
		}
	}
}

// Cart functionality with AJAX
$(document).ready(function () {
    // Attach event handler to all add-to-cart buttons
    $(document).on('click', '.add-to-cart-btn', function (e) {
        e.preventDefault();
        
        // Get product ID and return URL from the button's data attributes
        const productId = $(this).data('product-id');
        const returnUrl = $(this).data('return-url');
        
        // Show loading indicator
        $(this).prop('disabled', true);
        const originalText = $(this).html();
        $(this).html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Đang xử lý...');
        
        // Send AJAX request to add item to cart
        $.ajax({
            url: '/Customer/Cart/AddToCartAjax',
            type: 'POST',
            data: { id: productId, returnUrl: returnUrl },
            success: function (response) {
                if (response.success) {
                    // Update cart count badge
                    updateCartBadge(response.cartCount);
                    
                    // Show success toast
                    showToast('Thành công', response.message, 'success');
                } else {
                    // Show error toast
                    showToast('Lỗi', response.message, 'danger');
                }
            },
            error: function () {
                showToast('Lỗi', 'Đã xảy ra lỗi khi thêm sản phẩm vào giỏ hàng', 'danger');
            },
            complete: function () {
                // Restore button state
                $('.add-to-cart-btn').prop('disabled', false);
                $('.add-to-cart-btn').html(originalText);
            }
        });
    });
    
    // Function to update cart count badge
    function updateCartBadge(count) {
        const cartBadge = $('.cart-badge');
        cartBadge.text(count);
        
        if (count > 0) {
            cartBadge.addClass('has-items');
        } else {
            cartBadge.removeClass('has-items');
        }
        
        // Add animation effect
        cartBadge.addClass('badge-animation');
        setTimeout(function() {
            cartBadge.removeClass('badge-animation');
        }, 1000);
    }
    
    // Function to show toast notifications
    function showToast(title, message, type) {
        // Check if toast container exists, if not, create it
        if ($('#toast-container').length === 0) {
            $('body').append('<div id="toast-container" class="position-fixed top-0 end-0 p-3" style="z-index: 1100;"></div>');
        }
        
        // Create toast element
        const toastId = 'toast-' + Date.now();
        const toast = `
            <div id="${toastId}" class="toast align-items-center text-white bg-${type} border-0" role="alert" aria-live="assertive" aria-atomic="true">
                <div class="d-flex">
                    <div class="toast-body">
                        <strong>${title}</strong>: ${message}
                    </div>
                    <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
                </div>
            </div>
        `;
        
        // Add toast to container and show it
        $('#toast-container').append(toast);
        const toastElement = new bootstrap.Toast(document.getElementById(toastId), {
            delay: 3000
        });
        toastElement.show();
    }


    // Thêm function xử lý xóa sản phẩm bằng AJAX
    function removeItemFromCart(id) {
        if (!confirm("Bạn có chắc chắn muốn xóa sản phẩm này khỏi giỏ hàng?")) {
            return;
        }
        
        // Disable button and show loading
        const button = $(`button[data-remove-id="${id}"]`);
        const originalHtml = button.html();
        button.prop('disabled', true);
        button.html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>');
        
        $.ajax({
            url: '/Customer/Cart/RemoveFromCartAjax',
            type: 'POST',
            data: { id: id },
            success: function(response) {
                if (response.success) {
                    // Show message
                    showToast('Thông Báo', response.message, 'danger');
                    
                    // Remove row from table
                    const row = button.closest('tr.cart-item-row');
                    row.fadeOut(300, function() {
                        row.remove();
                        updateCartCount(response.cartCount);
                        updateTotalPrice();
                        
                        // If cart is empty, show empty cart message
                        if ($('.cart-item-row').length === 0) {
                            location.reload(); // Reload to show empty cart template
                        }
                    });
                } else {
                    showToast('Thông Báo', response.message, 'danger');
                    button.prop('disabled', false);
                    button.html(originalHtml);
                }
            },
            error: function() {
                showToast('Thông Báo', 'Đã xảy ra lỗi khi xóa sản phẩm', 'danger');
                button.prop('disabled', false);
                button.html(originalHtml);
            }
        });
    }
}); 
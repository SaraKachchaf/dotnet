import React, { createContext, useContext, useState, useEffect } from 'react';
import axios from '../api/axios';

const CartContext = createContext();

export const useCart = () => {
    const context = useContext(CartContext);
    if (!context) {
        throw new Error('useCart must be used within a CartProvider');
    }
    return context;
};

export const CartProvider = ({ children }) => {
    // Initial count from localStorage if needed, or start at 0
    const [cartCount, setCartCount] = useState(() => {
        const saved = localStorage.getItem('cartCount');
        return saved ? parseInt(saved, 10) : 0;
    });

    useEffect(() => {
        localStorage.setItem('cartCount', cartCount);
    }, [cartCount]);

    const refreshCount = async () => {
        const token = localStorage.getItem('token');
        if (!token) {
            setCartCount(0);
            return;
        }
        try {
            const res = await axios.get('/market/my-orders');
            const allOrders = res.data?.data || [];
            const pendingCount = allOrders.filter(o => ['pending', 'confirmed'].includes(o.status)).length;
            setCartCount(pendingCount);
        } catch (error) {
            console.error("Erreur sync panier:", error);
        }
    };

    useEffect(() => {
        refreshCount();
    }, []);

    const addToCart = () => {
        setCartCount(prev => prev + 1);
    };

    const removeFromCartCount = () => {
        setCartCount(prev => (prev > 0 ? prev - 1 : 0));
    };

    const clearCart = () => {
        setCartCount(0);
    };

    return (
        <CartContext.Provider value={{ cartCount, addToCart, removeFromCartCount, clearCart, refreshCount }}>
            {children}
        </CartContext.Provider>
    );
};

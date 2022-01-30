import Vue from 'vue'
import VueRouter, { RouteConfig } from 'vue-router'
import { Manager } from '@/main'
import { HubConnectionState } from '@microsoft/signalr'

Vue.use(VueRouter)

const routes: Array<RouteConfig> = [
	{
		path: '/',
		name: 'Index',
		beforeEnter: (to, from, next) => {
			document.title = "Home - ZigbeeHome";
			next();
		}
	},
	{
		path: '/editor',
		name: 'Editor',
		component: () => import("../views/Editor.vue"),
		beforeEnter: async (to, from, next) => {
			document.title = "Editor - ZigbeeHome";

			await Manager.connectionPromise;

			if(Manager.connection?.state === HubConnectionState.Connected)
			{
				next();
			}
			else
			{
				next('/');
			}
		}
	},
	{
		path: '/devices',
		name: 'Devices',
		component: () => import("../views/Devices.vue"),
		beforeEnter: async (to, from, next) => {
			document.title = "Devices - ZigbeeHome";
			
			await Manager.connectionPromise;

			if(Manager.connection?.state === HubConnectionState.Connected)
			{
				next();
			}
			else
			{
				next('/');
			}
		}
	},
]

const router = new VueRouter({
  mode: 'history',
  base: process.env.BASE_URL,
  routes
})

export default router

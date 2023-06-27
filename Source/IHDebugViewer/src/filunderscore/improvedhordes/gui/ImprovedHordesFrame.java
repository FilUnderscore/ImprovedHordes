package filunderscore.improvedhordes.gui;

import java.awt.Color;
import java.awt.Dimension;
import java.awt.event.ActionEvent;
import java.awt.event.ActionListener;
import java.awt.event.ComponentAdapter;
import java.awt.event.ComponentEvent;

import javax.swing.JButton;
import javax.swing.JFrame;
import javax.swing.JList;
import javax.swing.JScrollPane;

import filunderscore.improvedhordes.util.Vector2i;
import filunderscore.improvedhordes.world.ClusterSnapshot;
import filunderscore.improvedhordes.world.ImprovedHordesSimulation;
import filunderscore.improvedhordes.world.ImprovedHordesSimulation.ConnectionStatus;
import filunderscore.improvedhordes.world.PlayerSnapshot;

public final class ImprovedHordesFrame extends JFrame
{
	private static final long serialVersionUID = -3627411804693270145L;
	
	private ImprovedHordesSimulation simulation;
	private ImprovedHordesPanel panel;
	private Vector2i size;
	
	private final float LIST_PERCENT = 0.0f;
	private final float PANEL_PERCENT = 1.0f;
	
	private JList<ClusterSnapshot> clustersList;
	private JList<PlayerSnapshot> playersList;
	
	private JScrollPane clustersListScrollPane;
	private JScrollPane playersListScrollPane;
	
	private JButton reconnectButton;
	
	public ImprovedHordesFrame(Vector2i size, ImprovedHordesSimulation simulation)
	{
		this.simulation = simulation;
		this.size = size;
		
		this.setTitle("IHDebugViewer");
		
		this.setDefaultCloseOperation(JFrame.EXIT_ON_CLOSE);
		this.setSize(new Dimension(size.x, size.y));
		this.setVisible(true);
		
		this.addComponentListener(new ComponentAdapter()
		{
			public void componentResized(ComponentEvent componentEvent)
			{
				Dimension size = componentEvent.getComponent().getSize();
				ImprovedHordesFrame.this.size = getRealSize(new Vector2i(size.width, size.height));
				
				setSizes();
			}
		});
		
		this.setupComponents();
		this.setSizes();
	}
	
	private Vector2i getRealSize(Vector2i size)
	{
		return new Vector2i(size.x - (getInsets().left + getInsets().right), size.y - (getInsets().top + getInsets().bottom));
	}
	
	private void setupComponents()
	{
		setLayout(null);
		getContentPane().setBackground(Color.darkGray);
		
		this.panel = new ImprovedHordesPanel(simulation, new Vector2i(this.size.x * PANEL_PERCENT, this.size.y * PANEL_PERCENT));
		add(panel);
		
		panel.setBackground(Color.darkGray);
		panel.setForeground(Color.white);
		
		this.clustersList = new JList<ClusterSnapshot>();
		this.playersList = new JList<PlayerSnapshot>();
		
		this.clustersList.setBackground(Color.darkGray);
		this.playersList.setBackground(Color.darkGray);
		
		this.clustersList.setForeground(Color.white);
		this.playersList.setForeground(Color.white);

		this.clustersListScrollPane = new JScrollPane(this.clustersList);
		this.playersListScrollPane = new JScrollPane(this.playersList);

		this.reconnectButton = new JButton("Reconnect");
		this.reconnectButton.setBackground(Color.darkGray);
		this.reconnectButton.setForeground(Color.white);
		
		this.reconnectButton.addActionListener(new ActionListener()
		{
			@Override
			public void actionPerformed(ActionEvent e) 
			{
				simulation.status = ConnectionStatus.RECONNECTING;
			}			
		});
		
		add(this.clustersListScrollPane);
		add(this.playersListScrollPane);
		add(this.reconnectButton);
	}
	
	public void setSizes()
	{
		if(panel != null)
		{
			panel.setLocation((int)(this.size.x * LIST_PERCENT), 0);
			
			int maxSizeX = (int)(this.size.x * PANEL_PERCENT);
			int maxSizeY = (int)(this.size.y);

			int maxSize = Math.min(maxSizeX, maxSizeY);
			
			panel.setSize(new Vector2i(maxSize, maxSize));
		}

		if(this.clustersListScrollPane != null)
		{
			this.clustersListScrollPane.setLocation(0, 0);
			this.clustersListScrollPane.setSize((int)(this.size.x * LIST_PERCENT), this.size.y / 2);
		}
		
		if(this.playersListScrollPane != null)
		{
			this.playersListScrollPane.setLocation(0, this.size.y / 2);
			this.playersListScrollPane.setSize((int)(this.size.x * LIST_PERCENT), this.size.y / 2);
		}
		
		if(this.reconnectButton != null)
		{
			this.reconnectButton.setLocation((int)(this.size.x * LIST_PERCENT), (int)(2 * this.size.y / 3));
			this.reconnectButton.setSize((int)(this.size.x * PANEL_PERCENT), (int)(this.size.y / 3));
			
			this.reconnectButton.setVisible(simulation.status == ConnectionStatus.LOST_CONNECTION || 
					simulation.status == ConnectionStatus.NOT_CONNECTED);
		}
	}

	public void update()
	{
		if(simulation.world != null)
		{
			this.playersList.setListData(simulation.world.getPlayers().toArray(size -> new PlayerSnapshot[size]));
			this.clustersList.setListData(simulation.world.getClusters().toArray(size -> new ClusterSnapshot[size]));
		}
		
		this.reconnectButton.setVisible(simulation.status == ConnectionStatus.LOST_CONNECTION || 
				simulation.status == ConnectionStatus.NOT_CONNECTED);
	}
}
